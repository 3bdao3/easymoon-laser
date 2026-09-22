namespace ErpClink.Modules.Inventory.Application.Contracts;

public interface IWarehouseLookup
{
    Task<WarehouseSummaryLookup?> GetByIdAsync(Guid warehouseId, CancellationToken cancellationToken = default);
}

public sealed record WarehouseSummaryLookup(Guid Id, Guid OrganizationId, Guid BranchId, string WarehouseCode, string Name, bool IsActive);

public interface IInventoryItemLookup
{
    Task<InventoryItemSummaryLookup?> GetByIdAsync(Guid itemId, CancellationToken cancellationToken = default);
}

public sealed record InventoryItemSummaryLookup(
    Guid Id,
    Guid OrganizationId,
    string ItemCode,
    string Name,
    bool TrackExpiry,
    bool IsActive);

public interface IStockLookup
{
    Task<StockBalanceLookup?> GetBalanceAsync(Guid warehouseId, Guid inventoryItemId, CancellationToken cancellationToken = default);
}

public sealed record StockBalanceLookup(Guid WarehouseId, Guid InventoryItemId, decimal Quantity);

public interface IGoodsReceiptLookup
{
    Task<GoodsReceiptSummaryLookup?> GetByIdAsync(Guid goodsReceiptId, CancellationToken cancellationToken = default);
}

public sealed record GoodsReceiptSummaryLookup(
    Guid Id,
    string ReceiptNumber,
    Guid PurchaseOrderId,
    string Status);
