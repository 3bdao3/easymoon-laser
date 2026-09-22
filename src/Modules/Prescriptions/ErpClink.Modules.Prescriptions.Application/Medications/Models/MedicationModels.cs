namespace ErpClink.Modules.Prescriptions.Application.Medications.Models;

public sealed record CreateMedicationRequest(
    string Code,
    string Name,
    string? GenericName,
    string? Strength,
    string? DosageForm,
    string? Route);

public sealed record UpdateMedicationRequest(
    string Name,
    string? GenericName,
    string? Strength,
    string? DosageForm,
    string? Route);

public sealed record SearchMedicationsRequest(
    string? Q,
    string? Code,
    string? Name,
    string? GenericName,
    string? Strength,
    string? DosageForm,
    bool? ActiveOnly,
    int Page = 1,
    int PageSize = 20);

public sealed record MedicationDto(
    Guid Id,
    Guid OrganizationId,
    string Code,
    string Name,
    string? GenericName,
    string? Strength,
    string? DosageForm,
    string? Route,
    bool IsActive,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy);

public sealed record PagedMedicationsResult(
    IReadOnlyList<MedicationDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
