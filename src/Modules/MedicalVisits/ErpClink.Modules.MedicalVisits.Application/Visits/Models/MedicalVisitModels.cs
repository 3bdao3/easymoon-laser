namespace ErpClink.Modules.MedicalVisits.Application.Visits.Models;

public sealed record StartMedicalVisitRequest(Guid AppointmentId, Guid? QueueEntryId);

public sealed record UpdateClinicalNotesRequest(
    string? ChiefComplaint,
    string? ClinicalNotes,
    string? ExaminationFindings,
    string? DiagnosisNotes,
    string? FollowUpNotes,
    byte[]? RowVersion);

public sealed record CancelMedicalVisitRequest(string? Reason);

public sealed record SearchMedicalVisitsRequest(
    Guid? PatientId,
    Guid? DoctorId,
    Guid? ClinicId,
    Guid? AppointmentId,
    DateOnly? VisitDate,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? Status,
    string? VisitNumber,
    int Page = 1,
    int PageSize = 20);

public sealed record PatientHistoryRequest(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    Guid? DoctorId,
    Guid? ClinicId,
    string? Status,
    int Page = 1,
    int PageSize = 20);

public sealed record MedicalVisitDto(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    string VisitNumber,
    Guid PatientId,
    Guid AppointmentId,
    Guid DoctorId,
    Guid ClinicId,
    Guid? QueueEntryId,
    DateOnly VisitDate,
    string Status,
    string? ChiefComplaint,
    string? ClinicalNotes,
    string? ExaminationFindings,
    string? DiagnosisNotes,
    string? FollowUpNotes,
    DateTime? CompletedAtUtc,
    string? CompletedBy,
    DateTime? CancelledAtUtc,
    string? CancelledBy,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy,
    byte[] RowVersion);

public sealed record MedicalVisitListItemDto(
    Guid Id,
    string VisitNumber,
    DateOnly VisitDate,
    Guid PatientId,
    Guid DoctorId,
    Guid ClinicId,
    Guid AppointmentId,
    string Status,
    string? ChiefComplaint,
    string? DiagnosisNotes,
    string? FollowUpNotes);

public sealed record PagedMedicalVisitsResult(
    IReadOnlyList<MedicalVisitListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

/// <summary>
/// Derived patient visit context for Patient 360 (not a persisted patient status).
/// Relationship and treatment state are computed from visit history.
/// </summary>
public sealed record PatientVisitContextDto(
    int TotalVisits,
    int CompletedVisits,
    int ActiveVisits,
    bool IsReturningPatient,
    string PatientRelationship,
    string TreatmentState,
    string? CurrentEncounterKind,
    MedicalVisitListItemDto? ActiveVisit,
    MedicalVisitListItemDto? LastVisit,
    bool PreviousVisitHadFollowUpNotes);

public static class PatientVisitContextCodes
{
    public const string RelationshipNew = "New";
    public const string RelationshipReturning = "Returning";

    public const string TreatmentNone = "None";
    public const string TreatmentInProgress = "InTreatment";
    public const string TreatmentCompleted = "TreatmentCompleted";

    public const string EncounterFirstVisit = "FirstVisit";
    public const string EncounterFollowUp = "FollowUp";
    public const string EncounterConsultation = "Consultation";
}
