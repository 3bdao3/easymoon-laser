using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Inventory.Application.Costing;
using ErpClink.Modules.Inventory.Application.Stock;
using ErpClink.Modules.Inventory.Application.Stock.Models;
using ErpClink.Modules.Inventory.Domain.Costing;
using ErpClink.Modules.Inventory.Domain.Stock;
using ErpClink.Modules.Inventory.Infrastructure.Common;
using ErpClink.Modules.Inventory.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Inventory.Infrastructure.Stock;

public sealed class StockService : IStockService
{
    private readonly InventoryDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly IValidator<AdjustStockRequest> _adjustValidator;
    private readonly IInventoryValuationMethod _costing;

    public StockService(
        InventoryDbContext db,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        IValidator<AdjustStockRequest> adjustValidator,
        IInventoryValuationMethod costing)
    {
        _db = db;
        _org = org;
        _user = user;
        _clock = clock;
        _adjustValidator = adjustValidator;
        _costing = costing;
    }

    public async Task<PagedStockBalancesResult> SearchBalancesAsync(SearchStockBalancesRequest request, CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = _db.StockBalances.AsNoTracking().Where(b => b.OrganizationId == _org.OrganizationId);
        if (request.WarehouseId.HasValue) query = query.Where(b => b.WarehouseId == request.WarehouseId);
        if (request.InventoryItemId.HasValue) query = query.Where(b => b.InventoryItemId == request.InventoryItemId);
        query = query.OrderBy(b => b.WarehouseId).ThenBy(b => b.InventoryItemId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize).ToListAsync(cancellationToken);
        return new PagedStockBalancesResult(
            items.Select(b => new StockBalanceDto(
                b.Id, b.WarehouseId, b.InventoryItemId, b.Quantity, b.InventoryValue, b.AverageUnitCost, b.RowVersion)).ToList(),
            total,
            paging.NormalizedPage,
            paging.NormalizedPageSize);
    }

    public async Task<PagedStockMovementsResult> SearchMovementsAsync(
        SearchStockMovementsRequest request,
        CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = _db.StockMovements.AsNoTracking().Where(m => m.OrganizationId == _org.OrganizationId);
        if (request.WarehouseId.HasValue) query = query.Where(m => m.WarehouseId == request.WarehouseId);
        if (request.InventoryItemId.HasValue) query = query.Where(m => m.InventoryItemId == request.InventoryItemId);
        if (request.FromDate.HasValue)
        {
            var fromUtc = request.FromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(m => m.CreatedAtUtc >= fromUtc);
        }
        if (request.ToDate.HasValue)
        {
            var toExclusive = request.ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(m => m.CreatedAtUtc < toExclusive);
        }
        if (!string.IsNullOrWhiteSpace(request.MovementType) &&
            Enum.TryParse<StockMovementType>(request.MovementType, true, out var mt))
            query = query.Where(m => m.MovementType == mt);

        query = query.OrderByDescending(m => m.CreatedAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Skip).Take(paging.NormalizedPageSize)
            .Select(m => new StockMovementDto(
                m.Id, m.WarehouseId, m.InventoryItemId, m.BatchId, m.Quantity, m.MovementType.ToString(),
                m.ReferenceType, m.ReferenceId, m.Reason, m.BalanceAfter, m.CreatedAtUtc, m.CreatedBy))
            .ToListAsync(cancellationToken);
        return new PagedStockMovementsResult(items, total, paging.NormalizedPage, paging.NormalizedPageSize);
    }

    public async Task AdjustAsync(AdjustStockRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _adjustValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new AppException("inventory.validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        await EnsureWarehouseActiveAsync(request.WarehouseId, cancellationToken);
        await EnsureItemActiveAsync(request.InventoryItemId, cancellationToken);

        var balance = await GetOrCreateBalanceAsync(request.WarehouseId, request.InventoryItemId, cancellationToken);
        InventoryPersistenceHelper.ApplyRowVersion(_db, balance, request.BalanceRowVersion, b => b.RowVersion);

        var isIn = string.Equals(request.Direction, "In", StringComparison.OrdinalIgnoreCase);
        if (isIn && request.UnitCost is null)
            throw new AppException("inventory.unit_cost_required", "Unit cost is required for Adjust In so valuation is not invented.", 400);

        var referenceId = Guid.NewGuid();
        StockMovementType movementType;
        decimal balanceAfter;
        try
        {
            if (isIn)
            {
                balanceAfter = balance.Increase(request.Quantity);
                movementType = StockMovementType.AdjustmentIn;
            }
            else
            {
                if (request.BatchId.HasValue)
                    await DecreaseBatchAsync(request.BatchId.Value, request.Quantity, cancellationToken);
                balanceAfter = balance.Decrease(request.Quantity);
                movementType = StockMovementType.AdjustmentOut;
            }
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("inventory.negative_stock", ex.Message, 409);
        }

        var movement = StockMovement.Create(
            _org.OrganizationId,
            _org.BranchId,
            request.WarehouseId,
            request.InventoryItemId,
            request.BatchId,
            request.Quantity,
            movementType,
            "Adjustment",
            referenceId,
            request.Reason,
            balanceAfter,
            _user.UserId,
            _clock.UtcNow);
        _db.StockMovements.Add(movement);

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        if (isIn)
        {
            await _costing.ApplyReceiptAsync(new CostReceiptRequest(
                request.WarehouseId,
                today,
                "Adjustment",
                referenceId,
                "AdjustmentIn",
                $"adj-in:{referenceId:N}",
                Guid.NewGuid(),
                request.Reason,
                [new CostReceiptLineRequest(
                    request.InventoryItemId,
                    request.BatchId,
                    request.Quantity,
                    request.UnitCost!.Value,
                    movement.Id)],
                InventoryCostTransactionType.AdjustmentIn), cancellationToken);
        }
        else
        {
            await _costing.ApplyIssueAsync(new CostIssueRequest(
                request.WarehouseId,
                request.InventoryItemId,
                request.BatchId,
                request.Quantity,
                movement.Id,
                today,
                "Adjustment",
                referenceId,
                "AdjustmentOut",
                $"adj-out:{referenceId:N}",
                Guid.NewGuid(),
                request.Reason,
                InventoryCostTransactionType.AdjustmentOut), cancellationToken);
        }

        await InventoryPersistenceHelper.SaveChangesAsync(_db, "inventory.concurrency_conflict", cancellationToken);
    }

    private async Task DecreaseBatchAsync(Guid batchId, decimal quantity, CancellationToken cancellationToken)
    {
        var batch = await _db.StockBatches
            .Where(b => b.OrganizationId == _org.OrganizationId && b.Id == batchId)
            .SingleOrDefaultAsync(cancellationToken);
        if (batch is null)
            throw new AppException("inventory.batch_not_found", "Stock batch was not found.", 404);
        try
        {
            batch.Decrease(quantity);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("inventory.negative_batch", ex.Message, 409);
        }
    }

    private async Task<StockBalance> GetOrCreateBalanceAsync(Guid warehouseId, Guid itemId, CancellationToken cancellationToken)
    {
        var balance = await _db.StockBalances
            .Where(b => b.OrganizationId == _org.OrganizationId && b.WarehouseId == warehouseId && b.InventoryItemId == itemId)
            .SingleOrDefaultAsync(cancellationToken);
        if (balance is not null)
            return balance;

        balance = StockBalance.Create(_org.OrganizationId, _org.BranchId, warehouseId, itemId);
        _db.StockBalances.Add(balance);
        return balance;
    }

    private async Task EnsureWarehouseActiveAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        var warehouse = await _db.Warehouses.AsNoTracking()
            .SingleOrDefaultAsync(w => w.Id == warehouseId && w.OrganizationId == _org.OrganizationId, cancellationToken);
        if (warehouse is null)
            throw new AppException("inventory.warehouse_not_found", "Warehouse was not found.", 404);
        if (!warehouse.IsActive)
            throw new AppException("inventory.warehouse_inactive", "Warehouse is not active.", 409);
        if (warehouse.BranchId != _org.BranchId)
            throw new AppException("inventory.branch_mismatch", "Warehouse belongs to another branch.", 409);
    }

    private async Task EnsureItemActiveAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var item = await _db.InventoryItems.AsNoTracking()
            .SingleOrDefaultAsync(i => i.Id == itemId && i.OrganizationId == _org.OrganizationId, cancellationToken);
        if (item is null)
            throw new AppException("inventory.item_not_found", "Inventory item was not found.", 404);
        if (!item.IsActive)
            throw new AppException("inventory.item_inactive", "Inventory item is not active.", 409);
    }
}
