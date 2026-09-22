using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.Modules.Inventory.Application.Costing.Models;
using ErpClink.Modules.Inventory.Domain.Costing;
using ErpClink.Modules.Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Inventory.Infrastructure.Costing;

public sealed class InventoryValuationQueryService : IInventoryValuationQueryService
{
    private readonly InventoryDbContext _db;
    private readonly IOrganizationContext _org;

    public InventoryValuationQueryService(InventoryDbContext db, IOrganizationContext org)
    {
        _db = db;
        _org = org;
    }

    public async Task<ValuationResult> GetValuationAsync(ValuationQueryRequest request, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = from b in _db.StockBalances.AsNoTracking()
                    join i in _db.InventoryItems.AsNoTracking() on b.InventoryItemId equals i.Id
                    where b.OrganizationId == _org.OrganizationId && b.BranchId == _org.BranchId
                    select new { b, i };

        if (request.WarehouseId is { } wh) query = query.Where(x => x.b.WarehouseId == wh);
        if (request.InventoryItemId is { } item) query = query.Where(x => x.b.InventoryItemId == item);
        if (request.CategoryId is { } cat) query = query.Where(x => x.i.CategoryId == cat);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalQty = await query.SumAsync(x => x.b.Quantity, cancellationToken);
        var totalValue = await query.SumAsync(x => x.b.InventoryValue, cancellationToken);

        var rows = await query
            .OrderBy(x => x.i.ItemCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ValuationLineDto(
                x.b.WarehouseId,
                x.b.InventoryItemId,
                x.i.ItemCode,
                x.i.Name,
                x.b.Quantity,
                x.b.InventoryValue,
                x.b.Quantity > 0 ? x.b.InventoryValue / x.b.Quantity : null))
            .ToListAsync(cancellationToken);

        return new ValuationResult(
            totalQty,
            totalValue,
            InventoryValuationMethodCode.ProvisionalFifo.ToString(),
            rows,
            totalCount,
            page,
            pageSize);
    }

    public async Task<PagedCostHistoryResult> GetCostHistoryAsync(CostHistoryQueryRequest request, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var query = _db.InventoryCostTransactions.AsNoTracking()
            .Where(t => t.OrganizationId == _org.OrganizationId && t.InventoryItemId == request.InventoryItemId);
        if (request.WarehouseId is { } wh) query = query.Where(t => t.WarehouseId == wh);
        if (request.FromDate is { } from) query = query.Where(t => t.TransactionDate >= from);
        if (request.ToDate is { } to) query = query.Where(t => t.TransactionDate <= to);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new CostHistoryLineDto(
                t.Id, t.WarehouseId, t.TransactionType.ToString(), t.TransactionDate,
                t.Quantity, t.UnitCost, t.TotalCost, t.SourceType, t.SourceId, t.EventType,
                t.ValuationMethod.ToString(), t.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedCostHistoryResult(items, total, page, pageSize);
    }

    public async Task<PagedCostLayersResult> GetCostLayersAsync(CostLayerQueryRequest request, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var query = _db.InventoryCostLayers.AsNoTracking()
            .Where(l => l.OrganizationId == _org.OrganizationId && l.BranchId == _org.BranchId);
        if (request.WarehouseId is { } wh) query = query.Where(l => l.WarehouseId == wh);
        if (request.InventoryItemId is { } item) query = query.Where(l => l.InventoryItemId == item);
        if (request.StockBatchId is { } batch) query = query.Where(l => l.StockBatchId == batch);
        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<InventoryCostLayerStatus>(request.Status, true, out var status))
            query = query.Where(l => l.Status == status);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(l => l.ReceiptDate).ThenBy(l => l.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new CostLayerDto(
                l.Id, l.WarehouseId, l.InventoryItemId, l.StockBatchId, l.SourceType, l.SourceId,
                l.ReceiptDate, l.OriginalQuantity, l.RemainingQuantity, l.UnitCost, l.RemainingValue, l.Status.ToString()))
            .ToListAsync(cancellationToken);

        return new PagedCostLayersResult(items, total, page, pageSize);
    }

    public async Task<PagedIssueCostResult> GetIssueCostsAsync(IssueCostQueryRequest request, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var query = _db.InventoryCostTransactions.AsNoTracking()
            .Where(t => t.OrganizationId == _org.OrganizationId
                        && t.BranchId == _org.BranchId
                        && (t.TransactionType == InventoryCostTransactionType.Issue
                            || t.TransactionType == InventoryCostTransactionType.AdjustmentOut
                            || t.TransactionType == InventoryCostTransactionType.Reversal));
        if (request.WarehouseId is { } wh) query = query.Where(t => t.WarehouseId == wh);
        if (request.InventoryItemId is { } item) query = query.Where(t => t.InventoryItemId == item);
        if (request.FromDate is { } from) query = query.Where(t => t.TransactionDate >= from);
        if (request.ToDate is { } to) query = query.Where(t => t.TransactionDate <= to);

        var total = await query.CountAsync(cancellationToken);
        var totalCost = await query.SumAsync(t => t.TotalCost, cancellationToken);
        var items = await query
            .OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new IssueCostLineDto(
                t.Id, t.WarehouseId, t.InventoryItemId, t.TransactionDate,
                t.Quantity, t.UnitCost, t.TotalCost, t.SourceType, t.SourceId, t.EventType))
            .ToListAsync(cancellationToken);

        return new PagedIssueCostResult(items, totalCost, total, page, pageSize);
    }
}
