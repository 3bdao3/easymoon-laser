namespace ErpClink.Modules.Doctors.Application.Doctors.Models;

public sealed record RegisterDoctorRequest(
    string FirstName,
    string LastName,
    string? DisplayName,
    Guid? SpecialtyId,
    string? LicenseNumber,
    string? PhoneNumber,
    string? Email,
    string? UserId);

public sealed record UpdateDoctorRequest(
    string FirstName,
    string LastName,
    string? DisplayName,
    Guid? SpecialtyId,
    string? LicenseNumber,
    string? PhoneNumber,
    string? Email,
    string? UserId);

public sealed record SearchDoctorsRequest(
    string? Query,
    string? DoctorNumber,
    string? Name,
    string? Phone,
    string? Email,
    Guid? SpecialtyId,
    Guid? ClinicId,
    bool? IsActive,
    string? SortBy,
    bool SortDescending = false,
    int Page = 1,
    int PageSize = 20);

public sealed record AssignDoctorToClinicRequest(
    Guid ClinicId,
    DateOnly StartDate,
    DateOnly? EndDate);

public sealed record DoctorDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    string DoctorNumber,
    string? UserId,
    string FirstName,
    string LastName,
    string DisplayName,
    Guid? SpecialtyId,
    string? SpecialtyName,
    string? LicenseNumber,
    string? PhoneNumber,
    string? Email,
    bool IsActive,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy);

public sealed record DoctorListItemDto(
    Guid Id,
    string DoctorNumber,
    string DisplayName,
    Guid? SpecialtyId,
    string? SpecialtyName,
    string? PhoneNumber,
    string? Email,
    bool IsActive);

public sealed record DoctorClinicAssignmentDto(
    Guid Id,
    Guid DoctorId,
    Guid ClinicId,
    string ClinicCode,
    string ClinicName,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive);

public sealed record PagedDoctorsResult(
    IReadOnlyList<DoctorListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
