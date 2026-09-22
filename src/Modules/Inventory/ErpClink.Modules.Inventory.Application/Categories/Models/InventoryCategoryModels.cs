namespace ErpClink.Modules.Inventory.Application.Categories.Models;

public sealed record CreateInventoryCategoryRequest(string Code, string Name);
public sealed record UpdateInventoryCategoryRequest(string Name, byte[]? RowVersion);
public sealed record SearchInventoryCategoriesRequest(string? Query, bool? IsActive, int Page = 1, int PageSize = 20);

public sealed record InventoryCategoryDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive,
    byte[] RowVersion);

public sealed record PagedInventoryCategoriesResult(
    IReadOnlyList<InventoryCategoryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
