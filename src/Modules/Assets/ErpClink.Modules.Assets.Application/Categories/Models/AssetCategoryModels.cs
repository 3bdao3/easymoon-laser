namespace ErpClink.Modules.Assets.Application.Categories.Models;

public sealed record CreateAssetCategoryRequest(string Name, string? Description);
public sealed record UpdateAssetCategoryRequest(string Name, string? Description, byte[]? RowVersion);
public sealed record SearchAssetCategoriesRequest(string? Query, bool? IsActive, int Page = 1, int PageSize = 20);

public sealed record AssetCategoryDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    byte[] RowVersion);

public sealed record PagedAssetCategoriesResult(
    IReadOnlyList<AssetCategoryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
