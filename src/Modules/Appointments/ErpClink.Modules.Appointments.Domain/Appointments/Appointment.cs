using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Appointments.Domain.Appointments;

public enum AppointmentStatus
{
    Scheduled = 0,
    Confirmed = 1,
    Cancelled = 2,
    NoShow = 3,
    CheckedIn = 4
}

public sealed class AppointmentBookedDomainEvent : IDomainEvent
{
    public AppointmentBookedDomainEvent(Guid appointmentId, Guid patientId, Guid doctorId, Guid clinicId, DateTime occurredOnUtc)
    {
        AppointmentId = appointmentId;
        PatientId = patientId;
        DoctorId = doctorId;
        ClinicId = clinicId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid AppointmentId { get; }
    public Guid PatientId { get; }
    public Guid DoctorId { get; }
    public Guid ClinicId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class AppointmentConfirmedDomainEvent : IDomainEvent
{
    public AppointmentConfirmedDomainEvent(Guid appointmentId, DateTime occurredOnUtc)
    {
        AppointmentId = appointmentId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid AppointmentId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class AppointmentCancelledDomainEvent : IDomainEvent
{
    public AppointmentCancelledDomainEvent(Guid appointmentId, string? reason, DateTime occurredOnUtc)
    {
        AppointmentId = appointmentId;
        Reason = reason;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid AppointmentId { get; }
    public string? Reason { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class AppointmentRescheduledDomainEvent : IDomainEvent
{
    public AppointmentRescheduledDomainEvent(
        Guid appointmentId,
        DateOnly previousDate,
        TimeOnly previousStart,
        DateOnly newDate,
        TimeOnly newStart,
        DateTime occurredOnUtc)
    {
        AppointmentId = appointmentId;
        PreviousDate = previousDate;
        PreviousStart = previousStart;
        NewDate = newDate;
        NewStart = newStart;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid AppointmentId { get; }
    public DateOnly PreviousDate { get; }
    public TimeOnly PreviousStart { get; }
    public DateOnly NewDate { get; }
    public TimeOnly NewStart { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class AppointmentMarkedNoShowDomainEvent : IDomainEvent
{
    public AppointmentMarkedNoShowDomainEvent(Guid appointmentId, DateTime occurredOnUtc)
    {
        AppointmentId = appointmentId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid AppointmentId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class AppointmentCheckedInDomainEvent : IDomainEvent
{
    public AppointmentCheckedInDomainEvent(Guid appointmentId, DateTime occurredOnUtc)
    {
        AppointmentId = appointmentId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid AppointmentId { get; }
    public DateTime OccurredOnUtc { get; }
}

/// <summary>
/// Appointment booking aggregate. Clinical completion remains owned by Medical Visits.
/// </summary>
public sealed class Appointment : AggregateRoot
{
    private readonly List<AppointmentStatusHistoryEntry> _statusHistory = [];
    private readonly List<AppointmentRescheduleHistoryEntry> _rescheduleHistory = [];

    private Appointment()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public string AppointmentNumber { get; private set; } = string.Empty;
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid ClinicId { get; private set; }
    public DateOnly AppointmentDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public string? Notes { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancelledBy { get; private set; }

    public IReadOnlyCollection<AppointmentStatusHistoryEntry> StatusHistory => _statusHistory;
    public IReadOnlyCollection<AppointmentRescheduleHistoryEntry> RescheduleHistory => _rescheduleHistory;

    public bool OccupiesSlot => Status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed or AppointmentStatus.CheckedIn;

    public static Appointment Book(
        Guid organizationId,
        Guid branchId,
        string appointmentNumber,
        Guid patientId,
        Guid doctorId,
        Guid clinicId,
        DateOnly appointmentDate,
        TimeOnly startTime,
        TimeOnly endTime,
        string? reason,
        string? notes,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appointmentNumber);
        if (startTime >= endTime)
            throw new InvalidOperationException("StartTime must be before EndTime.");

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            AppointmentNumber = appointmentNumber.Trim(),
            PatientId = patientId,
            DoctorId = doctorId,
            ClinicId = clinicId,
            AppointmentDate = appointmentDate,
            StartTime = startTime,
            EndTime = endTime,
            Status = AppointmentStatus.Scheduled,
            Reason = Normalize(reason),
            Notes = Normalize(notes)
        };

        appointment.SetCreated(createdBy, utcNow);
        appointment.AddStatusHistory(AppointmentStatus.Scheduled, createdBy, utcNow, "Booked");
        appointment.RaiseDomainEvent(new AppointmentBookedDomainEvent(
            appointment.Id, patientId, doctorId, clinicId, utcNow));
        return appointment;
    }

    public void Confirm(string? updatedBy, DateTime utcNow)
    {
        EnsureTransition(AppointmentStatus.Confirmed);
        Status = AppointmentStatus.Confirmed;
        SetUpdated(updatedBy, utcNow);
        AddStatusHistory(AppointmentStatus.Confirmed, updatedBy, utcNow, null);
        RaiseDomainEvent(new AppointmentConfirmedDomainEvent(Id, utcNow));
    }

    public void Cancel(string? reason, string? updatedBy, DateTime utcNow)
    {
        EnsureTransition(AppointmentStatus.Cancelled);
        Status = AppointmentStatus.Cancelled;
        CancellationReason = Normalize(reason);
        CancelledAtUtc = utcNow;
        CancelledBy = updatedBy;
        SetUpdated(updatedBy, utcNow);
        AddStatusHistory(AppointmentStatus.Cancelled, updatedBy, utcNow, CancellationReason);
        RaiseDomainEvent(new AppointmentCancelledDomainEvent(Id, CancellationReason, utcNow));
    }

    public void CheckIn(string? updatedBy, DateTime utcNow)
    {
        if (Status is not (AppointmentStatus.Scheduled or AppointmentStatus.Confirmed))
            throw new InvalidOperationException($"Cannot check in appointment in status {Status}.");

        Status = AppointmentStatus.CheckedIn;
        SetUpdated(updatedBy, utcNow);
        AddStatusHistory(AppointmentStatus.CheckedIn, updatedBy, utcNow, null);
        RaiseDomainEvent(new AppointmentCheckedInDomainEvent(Id, utcNow));
    }

    public void MarkNoShow(string? updatedBy, DateTime utcNow)
    {
        EnsureTransition(AppointmentStatus.NoShow);
        Status = AppointmentStatus.NoShow;
        SetUpdated(updatedBy, utcNow);
        AddStatusHistory(AppointmentStatus.NoShow, updatedBy, utcNow, null);
        RaiseDomainEvent(new AppointmentMarkedNoShowDomainEvent(Id, utcNow));
    }

    public void Reschedule(
        DateOnly newDate,
        TimeOnly newStart,
        TimeOnly newEnd,
        string? updatedBy,
        DateTime utcNow)
    {
        if (Status is not (AppointmentStatus.Scheduled or AppointmentStatus.Confirmed))
            throw new InvalidOperationException($"Cannot reschedule appointment in status {Status}.");

        if (newStart >= newEnd)
            throw new InvalidOperationException("StartTime must be before EndTime.");

        var previousDate = AppointmentDate;
        var previousStart = StartTime;
        var previousEnd = EndTime;

        AppointmentDate = newDate;
        StartTime = newStart;
        EndTime = newEnd;
        SetUpdated(updatedBy, utcNow);

        _rescheduleHistory.Add(AppointmentRescheduleHistoryEntry.Create(
            Id, previousDate, previousStart, previousEnd, newDate, newStart, newEnd, updatedBy, utcNow));

        RaiseDomainEvent(new AppointmentRescheduledDomainEvent(
            Id, previousDate, previousStart, newDate, newStart, utcNow));
    }

    private void EnsureTransition(AppointmentStatus target)
    {
        var allowed = Status switch
        {
            AppointmentStatus.Scheduled => target is AppointmentStatus.Confirmed or AppointmentStatus.Cancelled,
            AppointmentStatus.Confirmed => target is AppointmentStatus.Cancelled or AppointmentStatus.NoShow,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException($"Cannot transition from {Status} to {target}.");
    }

    private void AddStatusHistory(AppointmentStatus status, string? by, DateTime utcNow, string? note)
    {
        _statusHistory.Add(AppointmentStatusHistoryEntry.Create(Id, status, by, utcNow, note));
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class AppointmentStatusHistoryEntry
{
    private AppointmentStatusHistoryEntry()
    {
    }

    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public string? ChangedBy { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }
    public string? Note { get; private set; }

    public static AppointmentStatusHistoryEntry Create(
        Guid appointmentId,
        AppointmentStatus status,
        string? changedBy,
        DateTime utcNow,
        string? note) =>
        new()
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            Status = status,
            ChangedBy = changedBy,
            ChangedAtUtc = utcNow,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };
}

public sealed class AppointmentRescheduleHistoryEntry
{
    private AppointmentRescheduleHistoryEntry()
    {
    }

    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public DateOnly PreviousDate { get; private set; }
    public TimeOnly PreviousStart { get; private set; }
    public TimeOnly PreviousEnd { get; private set; }
    public DateOnly NewDate { get; private set; }
    public TimeOnly NewStart { get; private set; }
    public TimeOnly NewEnd { get; private set; }
    public string? ChangedBy { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }

    public static AppointmentRescheduleHistoryEntry Create(
        Guid appointmentId,
        DateOnly previousDate,
        TimeOnly previousStart,
        TimeOnly previousEnd,
        DateOnly newDate,
        TimeOnly newStart,
        TimeOnly newEnd,
        string? changedBy,
        DateTime utcNow) =>
        new()
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            PreviousDate = previousDate,
            PreviousStart = previousStart,
            PreviousEnd = previousEnd,
            NewDate = newDate,
            NewStart = newStart,
            NewEnd = newEnd,
            ChangedBy = changedBy,
            ChangedAtUtc = utcNow
        };
}
