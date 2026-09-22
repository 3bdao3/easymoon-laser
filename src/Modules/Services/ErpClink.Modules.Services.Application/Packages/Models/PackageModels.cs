namespace ErpClink.Modules.Services.Application.Packages.Models;

public sealed record CreateHealthcarePackageRequest(
    string PackageCode,
    string Name,
    string? Description,
    IReadOnlyList<PackageItemInput>? Items);

public sealed record UpdateHealthcarePackageRequest(string Name, string? Description, byte[]? RowVersion);

public sealed record PackageItemInput(Guid ServiceId, decimal Quantity, int? SortOrder);

public sealed record UpdatePackageItemRequest(decimal Quantity, int SortOrder, byte[]? RowVersion);

public sealed record SearchHealthcarePackagesRequest(
    string? Query,
    string? PackageCode,
    bool? IsActive,
    int Page = 1,
    int PageSize = 20);

public sealed record PackageItemDto(
    Guid Id,
    Guid ServiceId,
    string ServiceCode,
    string ServiceName,
    decimal Quantity,
    int SortOrder);

public sealed record HealthcarePackageDto(
    Guid Id,
    Guid OrganizationId,
    string PackageCode,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy,
    byte[] RowVersion,
    IReadOnlyList<PackageItemDto> Items);

public sealed record HealthcarePackageListItemDto(
    Guid Id,
    string PackageCode,
    string Name,
    bool IsActive,
    int ItemCount);

public sealed record PagedHealthcarePackagesResult(
    IReadOnlyList<HealthcarePackageListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
