namespace ErpClink.Modules.Inventory.Application.Items.Models;

public sealed record CreateInventoryItemRequest(
    string Name,
    string? Description,
    Guid? CategoryId,
    string UnitOfMeasure,
    string? Barcode,
    bool TrackExpiry,
    decimal MinStockQuantity);

public sealed record UpdateInventoryItemRequest(
    string Name,
    string? Description,
    Guid? CategoryId,
    string UnitOfMeasure,
    string? Barcode,
    bool TrackExpiry,
    decimal MinStockQuantity,
    byte[]? RowVersion);

public sealed record SearchInventoryItemsRequest(string? Query, Guid? CategoryId, bool? IsActive, int Page = 1, int PageSize = 20);

public sealed record InventoryItemDto(
    Guid Id,
    string ItemCode,
    string Name,
    string? Description,
    Guid? CategoryId,
    string UnitOfMeasure,
    string? Barcode,
    bool TrackExpiry,
    decimal MinStockQuantity,
    bool IsActive,
    byte[] RowVersion);

public sealed record PagedInventoryItemsResult(
    IReadOnlyList<InventoryItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
