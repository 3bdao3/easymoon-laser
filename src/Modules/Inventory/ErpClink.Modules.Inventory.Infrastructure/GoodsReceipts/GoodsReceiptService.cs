using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Inventory.Application.Costing;
using ErpClink.Modules.Inventory.Application.GoodsReceipts;
using ErpClink.Modules.Inventory.Application.GoodsReceipts.Models;
using ErpClink.Modules.Inventory.Domain.GoodsReceipts;
using ErpClink.Modules.Inventory.Domain.Items;
using ErpClink.Modules.Inventory.Domain.Stock;
using ErpClink.Modules.Inventory.Infrastructure.Common;
using ErpClink.Modules.Inventory.Infrastructure.Persistence;
using ErpClink.Modules.Procurement.Application.Contracts;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Inventory.Infrastructure.GoodsReceipts;

public sealed class GoodsReceiptService : IGoodsReceiptService
{
    private readonly InventoryDbContext _db;
    private readonly IPurchaseOrderReceivingPort _poReceiving;
    private readonly IGoodsReceiptNumberGenerator _numbers;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IValidator<CreateGoodsReceiptFromPoRequest> _createValidator;
    private readonly IValidator<CancelGoodsReceiptRequest> _cancelValidator;
    private readonly IInventoryValuationMethod _costing;

    public GoodsReceiptService(
        InventoryDbContext db,
        IPurchaseOrderReceivingPort poReceiving,
        IGoodsReceiptNumberGenerator numbers,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IValidator<CreateGoodsReceiptFromPoRequest> createValidator,
        IValidator<CancelGoodsReceiptRequest> cancelValidator,
        IInventoryValuationMethod costing)
    {
        _db = db;
        _poReceiving = poReceiving;
        _numbers = numbers;
        _org = org;
        _user = user;
        _clock = clock;
        _createValidator = createValidator;
        _cancelValidator = cancelValidator;
        _costing = costing;
    }

    public async Task<GoodsReceiptDto> CreateFromPurchaseOrderAsync(
        CreateGoodsReceiptFromPoRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("inventory.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var warehouse = await _db.Warehouses.AsNoTracking()
            .SingleOrDefaultAsync(w => w.Id == request.WarehouseId && w.OrganizationId == _org.OrganizationId, cancellationToken)
            ?? throw new AppException("inventory.warehouse_not_found", "Warehouse was not found.", 404);
        if (!warehouse.IsActive)
            throw new AppException("inventory.warehouse_inactive", "Warehouse is not active.", 409);
        if (warehouse.BranchId != _org.BranchId)
            throw new AppException("inventory.branch_mismatch", "Warehouse belongs to another branch.", 409);

        var po = await _poReceiving.GetReceivableAsync(request.PurchaseOrderId, cancellationToken)
            ?? throw new AppException("procurement.purchase_order_not_receivable", "Purchase order is not receivable.", 409);

        if (po.BranchId != _org.BranchId)
            throw new AppException("inventory.branch_mismatch", "Purchase order belongs to another branch.", 409);

        var poLineMap = po.Lines.ToDictionary(l => l.LineId);
        var itemIds = request.Lines.Select(l => l.InventoryItemId).Distinct().ToList();
        var items = await _db.InventoryItems
            .Where(i => i.OrganizationId == _org.OrganizationId && itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var plannedLines = new List<(CreateGoodsReceiptLineRequest Input, InventoryItem Item, PurchaseOrderReceivingLineSnapshot PoLine, Guid? BatchId)>();
        var applyLines = new List<ApplyPurchaseOrderReceiptLine>();

        foreach (var input in request.Lines)
        {
            if (!items.TryGetValue(input.InventoryItemId, out var item))
                throw new AppException("inventory.item_not_found", "Inventory item was not found.", 404);
            if (!item.IsActive)
                throw new AppException("inventory.item_inactive", "Inventory item is not active.", 409);

            try
            {
                item.EnsureBatchRequired(input.BatchNumber, input.ExpiryDate);
            }
            catch (InvalidOperationException ex)
            {
                throw new AppException("inventory.batch_required", ex.Message, 400);
            }

            if (!poLineMap.TryGetValue(input.PurchaseOrderLineId, out var poLine))
                throw new AppException("procurement.line_not_found", "Purchase order line was not found.", 404);

            var remaining = poLine.QuantityOrdered - poLine.QuantityReceived;
            if (input.Quantity > remaining)
                throw new AppException("inventory.over_receive", "Receipt quantity exceeds remaining ordered quantity.", 409);

            applyLines.Add(new ApplyPurchaseOrderReceiptLine(input.PurchaseOrderLineId, input.Quantity));
            plannedLines.Add((input, item, poLine, null));
        }

        // Reserve PO quantity under optimistic concurrency before posting stock (no DTC).
        await _poReceiving.ApplyReceiptAsync(
            new ApplyPurchaseOrderReceiptRequest(po.Id, applyLines, request.PurchaseOrderRowVersion ?? po.RowVersion),
            cancellationToken);

        GoodsReceipt? posted = null;
        Exception? lastUnique = null;
        for (var attempt = 0; attempt < 25; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(10 * attempt, cancellationToken);

            _db.ChangeTracker.Clear();
            try
            {
                var lineInputs = new List<GoodsReceiptLineInput>();
                foreach (var planned in plannedLines)
                {
                    Guid? batchId = null;
                    if (planned.Item.TrackExpiry)
                    {
                        var batch = await UpsertBatchAsync(
                            request.WarehouseId,
                            planned.Input.InventoryItemId,
                            planned.Input.BatchNumber!,
                            planned.Input.ExpiryDate!.Value,
                            planned.Input.Quantity,
                            cancellationToken);
                        batchId = batch.Id;
                    }

                    lineInputs.Add(new GoodsReceiptLineInput(
                        planned.Input.PurchaseOrderLineId,
                        planned.Input.InventoryItemId,
                        planned.PoLine.DescriptionSnapshot,
                        planned.Input.Quantity,
                        planned.PoLine.UnitCost,
                        planned.Input.BatchNumber,
                        planned.Input.ExpiryDate,
                        batchId));
                }

                var receiptNumber = await _numbers.GenerateAsync(_org.OrganizationId, cancellationToken);
                var receipt = GoodsReceipt.CreatePosted(
                    _org.OrganizationId,
                    _org.BranchId,
                    receiptNumber,
                    request.WarehouseId,
                    po.Id,
                    po.SupplierId,
                    request.ReceiptDate,
                    request.Notes,
                    lineInputs,
                    _user.UserId,
                    _clock.UtcNow);

                _db.GoodsReceipts.Add(receipt);

                var costLines = new List<CostReceiptLineRequest>();
                foreach (var line in receipt.Lines)
                {
                    var balance = await GetOrCreateBalanceTrackedAsync(receipt.WarehouseId, line.InventoryItemId, cancellationToken);
                    var balanceAfter = balance.Increase(line.Quantity);

                    var movement = StockMovement.Create(
                        _org.OrganizationId,
                        _org.BranchId,
                        receipt.WarehouseId,
                        line.InventoryItemId,
                        line.StockBatchId,
                        line.Quantity,
                        StockMovementType.Receipt,
                        "GoodsReceipt",
                        receipt.Id,
                        null,
                        balanceAfter,
                        _user.UserId,
                        _clock.UtcNow);
                    _db.StockMovements.Add(movement);

                    costLines.Add(new CostReceiptLineRequest(
                        line.InventoryItemId,
                        line.StockBatchId,
                        line.Quantity,
                        line.UnitCostSnapshot,
                        movement.Id));
                }

                await using var inventoryTx = await _db.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    await _costing.ApplyReceiptAsync(new CostReceiptRequest(
                        receipt.WarehouseId,
                        receipt.ReceiptDate,
                        "GoodsReceipt",
                        receipt.Id,
                        "GoodsReceiptPosted",
                        $"gr:{receipt.Id:N}",
                        Guid.NewGuid(),
                        $"Goods receipt {receipt.ReceiptNumber}",
                        costLines), cancellationToken);

                    await InventoryPersistenceHelper.SaveChangesAsync(_db, "inventory.concurrency_conflict", cancellationToken);
                    await inventoryTx.CommitAsync(cancellationToken);
                }
                catch
                {
                    await inventoryTx.RollbackAsync(cancellationToken);
                    throw;
                }

                posted = receipt;
                break;
            }
            catch (DbUpdateException ex) when (InventoryPersistenceHelper.IsUniqueViolation(ex))
            {
                lastUnique = ex;
                // Concurrent StockBalance/batch insert — retry against persisted rows.
            }
            catch (AppException ex) when (ex.Code == "inventory.concurrency_conflict")
            {
                lastUnique = ex;
                // Concurrent RowVersion on StockBalance — retry.
            }
            catch
            {
                try
                {
                    await _poReceiving.ReverseReceiptAsync(
                        new ApplyPurchaseOrderReceiptRequest(po.Id, applyLines, null),
                        CancellationToken.None);
                }
                catch
                {
                }

                throw;
            }
        }

        if (posted is null)
        {
            try
            {
                await _poReceiving.ReverseReceiptAsync(
                    new ApplyPurchaseOrderReceiptRequest(po.Id, applyLines, null),
                    CancellationToken.None);
            }
            catch
            {
            }

            throw lastUnique is null
                ? new AppException("inventory.concurrency_conflict", "Could not post goods receipt under concurrency.", 409)
                : new AppException("inventory.concurrency_conflict", "Concurrent stock update; retry the receipt.", 409);
        }

        return Map(posted);
    }

    public async Task<GoodsReceiptDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var receipt = await OrgReceipts().AsNoTracking().Include(r => r.Lines)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        return receipt is null ? null : Map(receipt);
    }

    public async Task<PagedGoodsReceiptsResult> SearchAsync(SearchGoodsReceiptsRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        IQueryable<GoodsReceipt> query = OrgReceipts().AsNoTracking().Include(r => r.Lines);
        if (request.PurchaseOrderId.HasValue) query = query.Where(r => r.PurchaseOrderId == request.PurchaseOrderId);
        if (request.WarehouseId.HasValue) query = query.Where(r => r.WarehouseId == request.WarehouseId);
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<GoodsReceiptStatus>(request.Status, true, out var status))
            query = query.Where(r => r.Status == status);

        query = query.OrderByDescending(r => r.ReceiptDate).ThenByDescending(r => r.CreatedAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var page = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedGoodsReceiptsResult(page.Select(Map).ToList(), total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task<GoodsReceiptDto> CancelAsync(Guid id, CancelGoodsReceiptRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _cancelValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("inventory.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var receipt = await OrgReceipts().Include(r => r.Lines).SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new AppException("inventory.goods_receipt_not_found", "Goods receipt was not found.", 404);

        InventoryPersistenceHelper.ApplyRowVersion(_db, receipt, request.RowVersion, r => r.RowVersion);

        try
        {
            receipt.Cancel(request.Reason, _user.UserId, _clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("inventory.invalid_transition", ex.Message, 409);
        }

        foreach (var line in receipt.Lines)
        {
            var balance = await GetOrCreateBalanceTrackedAsync(receipt.WarehouseId, line.InventoryItemId, cancellationToken);
            decimal balanceAfter;
            try
            {
                balanceAfter = balance.Decrease(line.Quantity);
            }
            catch (InvalidOperationException ex)
            {
                throw new AppException("inventory.negative_stock", ex.Message, 409);
            }

            if (line.StockBatchId.HasValue)
            {
                var batch = await _db.StockBatches.SingleAsync(b => b.Id == line.StockBatchId.Value, cancellationToken);
                batch.Decrease(line.Quantity);
            }

            _db.StockMovements.Add(StockMovement.Create(
                _org.OrganizationId,
                _org.BranchId,
                receipt.WarehouseId,
                line.InventoryItemId,
                line.StockBatchId,
                line.Quantity,
                StockMovementType.Issue,
                "GoodsReceipt",
                receipt.Id,
                request.Reason ?? "Cancelled goods receipt",
                balanceAfter,
                _user.UserId,
                _clock.UtcNow));
        }

        await _costing.ReverseReceiptAsync(new CostReversalRequest(
            receipt.WarehouseId,
            receipt.Id,
            "GoodsReceipt",
            "GoodsReceiptCancelled",
            $"gr-cancel:{receipt.Id:N}",
            Guid.NewGuid(),
            request.Reason), cancellationToken);

        await InventoryPersistenceHelper.SaveChangesAsync(_db, "inventory.concurrency_conflict", cancellationToken);
        return Map(receipt);
    }

    private async Task<StockBatch> UpsertBatchAsync(
        Guid warehouseId,
        Guid itemId,
        string batchNumber,
        DateOnly expiryDate,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        var batch = await _db.StockBatches
            .Where(b => b.OrganizationId == _org.OrganizationId
                        && b.WarehouseId == warehouseId
                        && b.InventoryItemId == itemId
                        && b.BatchNumber == batchNumber)
            .SingleOrDefaultAsync(cancellationToken);

        if (batch is null)
        {
            batch = StockBatch.Create(_org.OrganizationId, warehouseId, itemId, batchNumber, expiryDate);
            _db.StockBatches.Add(batch);
        }
        else if (batch.ExpiryDate != expiryDate)
        {
            throw new AppException("inventory.batch_expiry_mismatch", "Batch number already exists with a different expiry date.", 409);
        }

        batch.Increase(quantity);
        return batch;
    }

    private async Task<StockBalance> GetOrCreateBalanceTrackedAsync(
        Guid warehouseId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var balance = _db.StockBalances.Local.FirstOrDefault(
            b => b.OrganizationId == _org.OrganizationId && b.WarehouseId == warehouseId && b.InventoryItemId == itemId);
        balance ??= await _db.StockBalances
            .Where(b => b.OrganizationId == _org.OrganizationId && b.WarehouseId == warehouseId && b.InventoryItemId == itemId)
            .SingleOrDefaultAsync(cancellationToken);
        if (balance is not null)
            return balance;

        balance = StockBalance.Create(_org.OrganizationId, _org.BranchId, warehouseId, itemId);
        _db.StockBalances.Add(balance);
        return balance;
    }

    private IQueryable<GoodsReceipt> OrgReceipts() =>
        _db.GoodsReceipts.Where(r => r.OrganizationId == _org.OrganizationId);

    private static GoodsReceiptDto Map(GoodsReceipt r) =>
        new(
            r.Id,
            r.ReceiptNumber,
            r.WarehouseId,
            r.PurchaseOrderId,
            r.SupplierId,
            r.ReceiptDate,
            r.Status.ToString(),
            r.Notes,
            r.PostedAtUtc,
            r.RowVersion,
            r.Lines.Select(l => new GoodsReceiptLineDto(
                l.Id,
                l.PurchaseOrderLineId,
                l.InventoryItemId,
                l.DescriptionSnapshot,
                l.Quantity,
                l.UnitCostSnapshot,
                l.BatchNumber,
                l.ExpiryDate,
                l.StockBatchId)).ToList());
}
