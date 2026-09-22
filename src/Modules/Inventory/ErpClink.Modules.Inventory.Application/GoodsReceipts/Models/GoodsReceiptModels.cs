namespace ErpClink.Modules.Inventory.Application.GoodsReceipts.Models;

public sealed record CreateGoodsReceiptLineRequest(
    Guid PurchaseOrderLineId,
    Guid InventoryItemId,
    decimal Quantity,
    string? BatchNumber,
    DateOnly? ExpiryDate);

public sealed record CreateGoodsReceiptFromPoRequest(
    Guid PurchaseOrderId,
    Guid WarehouseId,
    DateOnly ReceiptDate,
    string? Notes,
    byte[]? PurchaseOrderRowVersion,
    IReadOnlyList<CreateGoodsReceiptLineRequest> Lines);

public sealed record CancelGoodsReceiptRequest(string? Reason, byte[]? RowVersion);

public sealed record SearchGoodsReceiptsRequest(
    Guid? PurchaseOrderId,
    Guid? WarehouseId,
    string? Status,
    int Page = 1,
    int PageSize = 20);

public sealed record GoodsReceiptLineDto(
    Guid Id,
    Guid PurchaseOrderLineId,
    Guid InventoryItemId,
    string DescriptionSnapshot,
    decimal Quantity,
    decimal UnitCostSnapshot,
    string? BatchNumber,
    DateOnly? ExpiryDate,
    Guid? StockBatchId);

public sealed record GoodsReceiptDto(
    Guid Id,
    string ReceiptNumber,
    Guid WarehouseId,
    Guid PurchaseOrderId,
    Guid SupplierId,
    DateOnly ReceiptDate,
    string Status,
    string? Notes,
    DateTime? PostedAtUtc,
    byte[] RowVersion,
    IReadOnlyList<GoodsReceiptLineDto> Lines);

public sealed record PagedGoodsReceiptsResult(
    IReadOnlyList<GoodsReceiptDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
