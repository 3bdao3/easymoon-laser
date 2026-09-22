using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Inventory.Application.Costing;
using ErpClink.Modules.Inventory.Domain;
using ErpClink.Modules.Inventory.Domain.Costing;
using ErpClink.Modules.Inventory.Domain.Stock;
using ErpClink.Modules.Inventory.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.Inventory.Infrastructure.Costing;

/// <summary>
/// Provisional FIFO valuation. Not a finalized product decision — see ADR-015.
/// </summary>
public sealed class FifoInventoryValuationMethod : IInventoryValuationMethod
{
    private readonly InventoryDbContext _db;
    private readonly IOrganizationContext _org;
    private readonly ICurrentUser _user;
    private readonly IBusinessClock _clock;
    private readonly ILogger<FifoInventoryValuationMethod> _logger;

    public FifoInventoryValuationMethod(
        InventoryDbContext db,
        IOrganizationContext org,
        ICurrentUser user,
        IBusinessClock clock,
        ILogger<FifoInventoryValuationMethod> logger)
    {
        _db = db;
        _org = org;
        _user = user;
        _clock = clock;
        _logger = logger;
    }

    public InventoryValuationMethodCode Method => InventoryValuationMethodCode.ProvisionalFifo;

    public async Task<CostingResult> ApplyReceiptAsync(CostReceiptRequest request, CancellationToken cancellationToken = default)
    {
        var correlationId = request.CorrelationId is { } c && c != Guid.Empty ? c : Guid.NewGuid();
        var results = new List<CostingLineResult>();

        foreach (var line in request.Lines)
        {
            var idempotencyKey = $"{request.IdempotencyKeyPrefix}:{line.InventoryItemId:N}";
            var existing = await FindTxAsync(request.SourceType, request.SourceId, request.EventType, idempotencyKey, cancellationToken);
            if (existing is not null)
            {
                results.Add(MapExisting(existing));
                continue;
            }

            InventoryCost.EnsureNonNegativeUnitCost(line.UnitCost, nameof(line.UnitCost));
            Quantity.EnsurePositive(line.Quantity, nameof(line.Quantity));

            var layer = InventoryCostLayer.Create(
                _org.OrganizationId,
                _org.BranchId,
                request.WarehouseId,
                line.InventoryItemId,
                line.StockBatchId,
                request.SourceType,
                request.SourceId,
                request.TransactionDate,
                line.Quantity,
                line.UnitCost,
                _clock.UtcNow);

            _db.InventoryCostLayers.Add(layer);

            var total = layer.OriginalValue;
            var unit = layer.UnitCost;
            var tx = InventoryCostTransaction.Create(
                _org.OrganizationId,
                _org.BranchId,
                request.WarehouseId,
                line.InventoryItemId,
                line.StockMovementId,
                request.TransactionType,
                request.TransactionDate,
                line.Quantity,
                unit,
                total,
                request.SourceType,
                request.SourceId,
                request.EventType,
                correlationId,
                idempotencyKey,
                request.Description,
                Method,
                _user.UserId,
                _clock.UtcNow);
            tx.AddAllocation(layer.Id, line.Quantity, unit, total);
            _db.InventoryCostTransactions.Add(tx);

            var balance = await GetBalanceAsync(request.WarehouseId, line.InventoryItemId, cancellationToken);
            balance.IncreaseValue(total);

            results.Add(new CostingLineResult(
                line.InventoryItemId, line.Quantity, unit, total, tx.Id,
                [new CostAllocationResult(layer.Id, line.Quantity, unit, total)]));
        }

        return new CostingResult(false, results);
    }

    public async Task<CostingResult> ApplyIssueAsync(CostIssueRequest request, CancellationToken cancellationToken = default)
    {
        var correlationId = request.CorrelationId is { } c && c != Guid.Empty ? c : Guid.NewGuid();
        var existing = await FindTxAsync(request.SourceType, request.SourceId, request.EventType, request.IdempotencyKey, cancellationToken);
        if (existing is not null)
            return new CostingResult(true, [MapExisting(existing)]);

        Quantity.EnsurePositive(request.Quantity, nameof(request.Quantity));
        var remainingToIssue = Quantity.Round(request.Quantity);

        var layersQuery = _db.InventoryCostLayers
            .Where(l => l.OrganizationId == _org.OrganizationId
                        && l.WarehouseId == request.WarehouseId
                        && l.InventoryItemId == request.InventoryItemId
                        && l.Status == InventoryCostLayerStatus.Open
                        && l.RemainingQuantity > 0);

        if (request.StockBatchId is { } batchId)
            layersQuery = layersQuery.Where(l => l.StockBatchId == batchId);

        var layers = await layersQuery
            .OrderBy(l => l.ReceiptDate)
            .ThenBy(l => l.CreatedAtUtc)
            .ThenBy(l => l.Id)
            .ToListAsync(cancellationToken);

        var available = Quantity.Round(layers.Sum(l => l.RemainingQuantity));
        if (available < remainingToIssue)
            throw new AppException(
                "inventory.insufficient_costed_stock",
                $"Insufficient costed stock. Available costed quantity {available}; requested {remainingToIssue}. Legacy unvalued stock cannot be costed automatically.",
                409);

        var allocations = new List<(InventoryCostLayer Layer, decimal Qty, decimal Value)>();
        foreach (var layer in layers)
        {
            if (remainingToIssue <= 0) break;
            var take = Math.Min(layer.RemainingQuantity, remainingToIssue);
            var value = layer.Consume(take);
            allocations.Add((layer, take, value));
            remainingToIssue = Quantity.Round(remainingToIssue - take);
        }

        var totalCost = InventoryCost.RoundMoney(allocations.Sum(a => a.Value));
        var unitCost = request.Quantity == 0
            ? 0
            : InventoryCost.RoundUnitCost(totalCost / Quantity.Round(request.Quantity));

        var tx = InventoryCostTransaction.Create(
            _org.OrganizationId,
            _org.BranchId,
            request.WarehouseId,
            request.InventoryItemId,
            request.StockMovementId,
            request.TransactionType,
            request.TransactionDate,
            request.Quantity,
            unitCost,
            totalCost,
            request.SourceType,
            request.SourceId,
            request.EventType,
            correlationId,
            request.IdempotencyKey,
            request.Description,
            Method,
            _user.UserId,
            _clock.UtcNow);

        foreach (var a in allocations)
            tx.AddAllocation(a.Layer.Id, a.Qty, a.Layer.UnitCost, a.Value);

        _db.InventoryCostTransactions.Add(tx);

        var balance = await GetBalanceAsync(request.WarehouseId, request.InventoryItemId, cancellationToken);
        balance.DecreaseValue(totalCost);

        return new CostingResult(false, [
            new CostingLineResult(
                request.InventoryItemId,
                request.Quantity,
                unitCost,
                totalCost,
                tx.Id,
                allocations.Select(a => new CostAllocationResult(a.Layer.Id, a.Qty, a.Layer.UnitCost, a.Value)).ToList())
        ]);
    }

    public async Task<CostingResult> ReverseReceiptAsync(CostReversalRequest request, CancellationToken cancellationToken = default)
    {
        var correlationId = request.CorrelationId is { } c && c != Guid.Empty ? c : Guid.NewGuid();
        var existing = await FindTxAsync(request.SourceType, request.SourceId, request.EventType, request.IdempotencyKey, cancellationToken);
        if (existing is not null)
            return new CostingResult(true, [MapExisting(existing)]);

        var layers = await _db.InventoryCostLayers
            .Where(l => l.OrganizationId == _org.OrganizationId
                        && l.WarehouseId == request.WarehouseId
                        && l.SourceType == request.SourceType
                        && l.SourceId == request.SourceId
                        && l.Status != InventoryCostLayerStatus.Reversed)
            .ToListAsync(cancellationToken);

        if (layers.Count == 0)
            return new CostingResult(false, []);

        var results = new List<CostingLineResult>();
        foreach (var layer in layers)
        {
            if (layer.RemainingQuantity != layer.OriginalQuantity)
                throw new AppException(
                    "inventory.cost_layer_consumed",
                    "Cannot reverse receipt costing because cost layers were partially or fully consumed by later issues.",
                    409);

            var qty = layer.RemainingQuantity;
            var value = layer.RemainingValue;
            var unit = layer.UnitCost;
            layer.MarkReversed();

            var tx = InventoryCostTransaction.Create(
                _org.OrganizationId,
                _org.BranchId,
                request.WarehouseId,
                layer.InventoryItemId,
                null,
                InventoryCostTransactionType.Reversal,
                DateOnly.FromDateTime(_clock.UtcNow),
                qty,
                unit,
                value,
                request.SourceType,
                request.SourceId,
                request.EventType,
                correlationId,
                $"{request.IdempotencyKey}:{layer.Id:N}",
                request.Description,
                Method,
                _user.UserId,
                _clock.UtcNow);
            tx.AddAllocation(layer.Id, qty, unit, value);
            _db.InventoryCostTransactions.Add(tx);

            var balance = await GetBalanceAsync(request.WarehouseId, layer.InventoryItemId, cancellationToken);
            balance.DecreaseValue(value);

            results.Add(new CostingLineResult(
                layer.InventoryItemId, qty, unit, value, tx.Id,
                [new CostAllocationResult(layer.Id, qty, unit, value)]));
        }

        return new CostingResult(false, results);
    }

    private async Task<StockBalance> GetBalanceAsync(Guid warehouseId, Guid itemId, CancellationToken cancellationToken)
    {
        // Prefer the change-tracker: newly created balances exist in Local before SaveChanges.
        var balance = _db.StockBalances.Local.FirstOrDefault(
            b => b.OrganizationId == _org.OrganizationId && b.WarehouseId == warehouseId && b.InventoryItemId == itemId);
        balance ??= await _db.StockBalances
            .SingleOrDefaultAsync(
                b => b.OrganizationId == _org.OrganizationId && b.WarehouseId == warehouseId && b.InventoryItemId == itemId,
                cancellationToken);
        if (balance is null)
            throw new AppException("inventory.stock_balance_missing", "Stock balance must exist before costing.", 409);
        return balance;
    }

    private Task<InventoryCostTransaction?> FindTxAsync(
        string sourceType,
        Guid sourceId,
        string eventType,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        _db.InventoryCostTransactions
            .Include(t => t.Allocations)
            .FirstOrDefaultAsync(
                t => t.OrganizationId == _org.OrganizationId
                     && t.SourceType == sourceType
                     && t.SourceId == sourceId
                     && t.EventType == eventType
                     && t.IdempotencyKey == idempotencyKey,
                cancellationToken);

    private static CostingLineResult MapExisting(InventoryCostTransaction tx) =>
        new(
            tx.InventoryItemId,
            tx.Quantity,
            tx.UnitCost,
            tx.TotalCost,
            tx.Id,
            tx.Allocations.Select(a => new CostAllocationResult(a.CostLayerId, a.Quantity, a.UnitCost, a.TotalCost)).ToList());
}
