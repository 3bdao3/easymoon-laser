using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.Modules.Inventory.Application.Contracts;
using ErpClink.Modules.Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Inventory.Infrastructure.Contracts;

public sealed class WarehouseLookup : IWarehouseLookup
{
    private readonly InventoryDbContext _db;
    private readonly IOrganizationContext _org;

    public WarehouseLookup(InventoryDbContext db, IOrganizationContext org)
    {
        _db = db;
        _org = org;
    }

    public async Task<WarehouseSummaryLookup?> GetByIdAsync(Guid warehouseId, CancellationToken cancellationToken = default) =>
        await _db.Warehouses.AsNoTracking()
            .Where(w => w.Id == warehouseId && w.OrganizationId == _org.OrganizationId)
            .Select(w => new WarehouseSummaryLookup(w.Id, w.OrganizationId, w.BranchId, w.WarehouseCode, w.Name, w.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
}

public sealed class InventoryItemLookup : IInventoryItemLookup
{
    private readonly InventoryDbContext _db;
    private readonly IOrganizationContext _org;

    public InventoryItemLookup(InventoryDbContext db, IOrganizationContext org)
    {
        _db = db;
        _org = org;
    }

    public async Task<InventoryItemSummaryLookup?> GetByIdAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        await _db.InventoryItems.AsNoTracking()
            .Where(i => i.Id == itemId && i.OrganizationId == _org.OrganizationId)
            .Select(i => new InventoryItemSummaryLookup(i.Id, i.OrganizationId, i.ItemCode, i.Name, i.TrackExpiry, i.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
}

public sealed class StockLookup : IStockLookup
{
    private readonly InventoryDbContext _db;
    private readonly IOrganizationContext _org;

    public StockLookup(InventoryDbContext db, IOrganizationContext org)
    {
        _db = db;
        _org = org;
    }

    public async Task<StockBalanceLookup?> GetBalanceAsync(
        Guid warehouseId,
        Guid inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        var balance = await _db.StockBalances.AsNoTracking()
            .Where(b => b.OrganizationId == _org.OrganizationId && b.WarehouseId == warehouseId && b.InventoryItemId == inventoryItemId)
            .Select(b => new StockBalanceLookup(b.WarehouseId, b.InventoryItemId, b.Quantity))
            .SingleOrDefaultAsync(cancellationToken);
        return balance;
    }
}

public sealed class GoodsReceiptLookup : IGoodsReceiptLookup
{
    private readonly InventoryDbContext _db;
    private readonly IOrganizationContext _org;

    public GoodsReceiptLookup(InventoryDbContext db, IOrganizationContext org)
    {
        _db = db;
        _org = org;
    }

    public async Task<GoodsReceiptSummaryLookup?> GetByIdAsync(Guid goodsReceiptId, CancellationToken cancellationToken = default) =>
        await _db.GoodsReceipts.AsNoTracking()
            .Where(r => r.Id == goodsReceiptId && r.OrganizationId == _org.OrganizationId)
            .Select(r => new GoodsReceiptSummaryLookup(r.Id, r.ReceiptNumber, r.PurchaseOrderId, r.Status.ToString()))
            .SingleOrDefaultAsync(cancellationToken);
}
