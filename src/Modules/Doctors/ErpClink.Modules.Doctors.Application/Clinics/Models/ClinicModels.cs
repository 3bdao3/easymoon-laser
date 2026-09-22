namespace ErpClink.Modules.Doctors.Application.Clinics.Models;

public sealed record CreateClinicRequest(
    string Code,
    string Name,
    string? Description,
    string? Location);

public sealed record UpdateClinicRequest(
    string Name,
    string? Description,
    string? Location);

public sealed record SearchClinicsRequest(
    string? Query,
    string? Code,
    string? Name,
    Guid? BranchId,
    bool? IsActive,
    string? SortBy,
    bool SortDescending = false,
    int Page = 1,
    int PageSize = 20);

public sealed record ClinicDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    string Code,
    string Name,
    string? Description,
    string? Location,
    bool IsActive,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy);

public sealed record ClinicListItemDto(
    Guid Id,
    string Code,
    string Name,
    string? Location,
    bool IsActive);

public sealed record PagedClinicsResult(
    IReadOnlyList<ClinicListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
