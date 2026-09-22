using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Patients.Domain.Patients;

namespace ErpClink.Modules.Patients.Application.Patients.Models;

public sealed record RegisterPatientRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    string? NationalId,
    string PhoneNumber,
    string? Email,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    bool AllowPossibleDuplicate = false);

public sealed record UpdatePatientRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    string? NationalId,
    string PhoneNumber,
    string? Email,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? EmergencyContactName,
    string? EmergencyContactPhone);

public sealed record SearchPatientsRequest(
    string? Query,
    string? PatientNumber,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? NationalId,
    bool? IsActive,
    string? SortBy,
    bool SortDescending = false,
    int Page = 1,
    int PageSize = 20);

public sealed record AddAllergyRequest(
    string Name,
    string? Reaction,
    AllergySeverity Severity,
    string? Notes);

public sealed record UpdateAllergyRequest(
    string Name,
    string? Reaction,
    AllergySeverity Severity,
    string? Notes);

public sealed record AllergyDto(
    Guid Id,
    string Name,
    string? Reaction,
    AllergySeverity Severity,
    string? Notes,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record MedicalHistoryItemDto(
    Guid Id,
    string Category,
    string Description,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record PatientDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    string PatientNumber,
    string FirstName,
    string? MiddleName,
    string LastName,
    string FullName,
    DateOnly DateOfBirth,
    Gender Gender,
    string? NationalId,
    string PhoneNumber,
    string? Email,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    bool IsActive,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy,
    IReadOnlyList<AllergyDto> Allergies,
    IReadOnlyList<MedicalHistoryItemDto> MedicalHistory);

public sealed record PatientListItemDto(
    Guid Id,
    string PatientNumber,
    string FullName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string? NationalId,
    bool IsActive);

public sealed record RegisterPatientResult(
    PatientDto Patient,
    IReadOnlyList<PatientListItemDto> PossibleDuplicates);

public sealed record PagedPatientsResult(
    IReadOnlyList<PatientListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public static PagedPatientsResult From(PagedResult<PatientListItemDto> page) =>
        new(page.Items, page.Page, page.PageSize, page.TotalCount);
}
