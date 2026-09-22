namespace ErpClink.Modules.Services.Application.Categories.Models;

public sealed record CreateServiceCategoryRequest(string Code, string Name, string? Description);

public sealed record UpdateServiceCategoryRequest(string Name, string? Description, byte[]? RowVersion);

public sealed record SearchServiceCategoriesRequest(string? Query, bool? IsActive, int Page = 1, int PageSize = 20);

public sealed record ServiceCategoryDto(
    Guid Id,
    Guid OrganizationId,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy,
    byte[] RowVersion);

public sealed record PagedServiceCategoriesResult(
    IReadOnlyList<ServiceCategoryDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
