using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.MedicalVisits.Domain.Visits;

public enum MedicalVisitStatus
{
    Open = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public sealed class MedicalVisitStartedDomainEvent : IDomainEvent
{
    public MedicalVisitStartedDomainEvent(Guid medicalVisitId, Guid appointmentId, string visitNumber, DateTime occurredOnUtc)
    {
        MedicalVisitId = medicalVisitId;
        AppointmentId = appointmentId;
        VisitNumber = visitNumber;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid MedicalVisitId { get; }
    public Guid AppointmentId { get; }
    public string VisitNumber { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class MedicalVisitClinicalNotesUpdatedDomainEvent : IDomainEvent
{
    public MedicalVisitClinicalNotesUpdatedDomainEvent(Guid medicalVisitId, DateTime occurredOnUtc)
    {
        MedicalVisitId = medicalVisitId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid MedicalVisitId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class MedicalVisitCompletedDomainEvent : IDomainEvent
{
    public MedicalVisitCompletedDomainEvent(Guid medicalVisitId, DateTime occurredOnUtc)
    {
        MedicalVisitId = medicalVisitId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid MedicalVisitId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class MedicalVisitCancelledDomainEvent : IDomainEvent
{
    public MedicalVisitCancelledDomainEvent(Guid medicalVisitId, DateTime occurredOnUtc)
    {
        MedicalVisitId = medicalVisitId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid MedicalVisitId { get; }
    public DateTime OccurredOnUtc { get; }
}

/// <summary>
/// Clinical encounter for a checked-in appointment.
/// MedicalVisit.Completed ≠ Queue.Completed (clinical vs operational).
/// </summary>
public sealed class MedicalVisit : AggregateRoot
{
    private MedicalVisit()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public string VisitNumber { get; private set; } = string.Empty;
    public Guid PatientId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid? QueueEntryId { get; private set; }
    public DateOnly VisitDate { get; private set; }
    public MedicalVisitStatus Status { get; private set; }
    public string? ChiefComplaint { get; private set; }
    public string? ClinicalNotes { get; private set; }
    public string? ExaminationFindings { get; private set; }
    public string? DiagnosisNotes { get; private set; }
    public string? FollowUpNotes { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? CompletedBy { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancelledBy { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public bool IsActive => Status is MedicalVisitStatus.Open or MedicalVisitStatus.InProgress;

    public static MedicalVisit Start(
        Guid organizationId,
        Guid branchId,
        string visitNumber,
        Guid patientId,
        Guid appointmentId,
        Guid doctorId,
        Guid clinicId,
        Guid? queueEntryId,
        DateOnly visitDate,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(visitNumber);

        var visit = new MedicalVisit
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            VisitNumber = visitNumber.Trim(),
            PatientId = patientId,
            AppointmentId = appointmentId,
            DoctorId = doctorId,
            ClinicId = clinicId,
            QueueEntryId = queueEntryId,
            VisitDate = visitDate,
            Status = MedicalVisitStatus.Open
        };
        visit.SetCreated(createdBy, utcNow);
        visit.RaiseDomainEvent(new MedicalVisitStartedDomainEvent(visit.Id, appointmentId, visit.VisitNumber, utcNow));
        return visit;
    }

    public void BeginProgress(string? updatedBy, DateTime utcNow)
    {
        EnsureTransition(MedicalVisitStatus.InProgress);
        Status = MedicalVisitStatus.InProgress;
        SetUpdated(updatedBy, utcNow);
    }

    public void UpdateClinicalNotes(
        string? chiefComplaint,
        string? clinicalNotes,
        string? examinationFindings,
        string? diagnosisNotes,
        string? followUpNotes,
        string? updatedBy,
        DateTime utcNow)
    {
        if (Status is MedicalVisitStatus.Completed or MedicalVisitStatus.Cancelled)
            throw new InvalidOperationException($"Cannot update clinical notes for visit in status {Status}.");

        if (Status == MedicalVisitStatus.Open)
            Status = MedicalVisitStatus.InProgress;

        ChiefComplaint = Normalize(chiefComplaint, 1000);
        ClinicalNotes = Normalize(clinicalNotes, 4000);
        ExaminationFindings = Normalize(examinationFindings, 4000);
        DiagnosisNotes = Normalize(diagnosisNotes, 2000);
        FollowUpNotes = Normalize(followUpNotes, 2000);
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new MedicalVisitClinicalNotesUpdatedDomainEvent(Id, utcNow));
    }

    public void Complete(string? updatedBy, DateTime utcNow)
    {
        EnsureTransition(MedicalVisitStatus.Completed);
        Status = MedicalVisitStatus.Completed;
        CompletedAtUtc = utcNow;
        CompletedBy = updatedBy;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new MedicalVisitCompletedDomainEvent(Id, utcNow));
    }

    public void Cancel(string? reason, string? updatedBy, DateTime utcNow)
    {
        EnsureTransition(MedicalVisitStatus.Cancelled);
        Status = MedicalVisitStatus.Cancelled;
        CancellationReason = Normalize(reason, 500);
        CancelledAtUtc = utcNow;
        CancelledBy = updatedBy;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new MedicalVisitCancelledDomainEvent(Id, utcNow));
    }

    private void EnsureTransition(MedicalVisitStatus target)
    {
        var allowed = Status switch
        {
            MedicalVisitStatus.Open => target is MedicalVisitStatus.InProgress or MedicalVisitStatus.Completed or MedicalVisitStatus.Cancelled,
            MedicalVisitStatus.InProgress => target is MedicalVisitStatus.Completed or MedicalVisitStatus.Cancelled,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException($"Cannot transition from {Status} to {target}.");
    }

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
