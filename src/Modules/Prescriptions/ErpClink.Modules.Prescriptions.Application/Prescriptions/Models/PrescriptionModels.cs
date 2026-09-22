namespace ErpClink.Modules.Prescriptions.Application.Prescriptions.Models;

public sealed record PrescriptionItemInput(
    Guid MedicationId,
    string Dosage,
    string Frequency,
    string? Duration,
    string? Route,
    string? Instructions,
    decimal? Quantity,
    string? Notes,
    int? SortOrder);

public sealed record CreatePrescriptionRequest(
    Guid MedicalVisitId,
    string? Notes,
    IReadOnlyList<PrescriptionItemInput>? Items);

public sealed record UpdatePrescriptionRequest(string? Notes, byte[]? RowVersion);

public sealed record UpdatePrescriptionItemRequest(
    string Dosage,
    string Frequency,
    string? Duration,
    string? Route,
    string? Instructions,
    decimal? Quantity,
    string? Notes,
    int SortOrder,
    byte[]? RowVersion);

public sealed record CancelPrescriptionRequest(string? Reason);

public sealed record SearchPrescriptionsRequest(
    Guid? PatientId,
    Guid? DoctorId,
    Guid? MedicalVisitId,
    string? PrescriptionNumber,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? Status,
    int Page = 1,
    int PageSize = 20);

public sealed record PatientPrescriptionHistoryRequest(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? Status,
    int Page = 1,
    int PageSize = 20);

public sealed record PrescriptionItemDto(
    Guid Id,
    Guid MedicationId,
    string MedicationNameSnapshot,
    string Dosage,
    string Frequency,
    string? Duration,
    string? Route,
    string? Instructions,
    decimal? Quantity,
    string? Notes,
    int SortOrder);

public sealed record PrescriptionDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    string PrescriptionNumber,
    Guid MedicalVisitId,
    Guid PatientId,
    Guid DoctorId,
    DateOnly PrescriptionDate,
    string Status,
    string? Notes,
    DateTime? IssuedAtUtc,
    string? IssuedBy,
    DateTime? CancelledAtUtc,
    string? CancelledBy,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy,
    byte[] RowVersion,
    IReadOnlyList<PrescriptionItemDto> Items);

public sealed record PrescriptionListItemDto(
    Guid Id,
    string PrescriptionNumber,
    DateOnly PrescriptionDate,
    Guid PatientId,
    Guid DoctorId,
    Guid MedicalVisitId,
    string Status,
    int ItemCount);

public sealed record PagedPrescriptionsResult(
    IReadOnlyList<PrescriptionListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
