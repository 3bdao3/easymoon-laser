namespace ErpClink.Modules.Assets.Application.Locations.Models;

public sealed record CreateAssetLocationRequest(string Name, string? Description);
public sealed record UpdateAssetLocationRequest(string Name, string? Description, byte[]? RowVersion);
public sealed record SearchAssetLocationsRequest(string? Query, bool? IsActive, int Page = 1, int PageSize = 20);

public sealed record AssetLocationDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    byte[] RowVersion);

public sealed record PagedAssetLocationsResult(
    IReadOnlyList<AssetLocationDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
