using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Queue.Domain.Entries;

public enum QueueStatus
{
    Waiting = 0,
    Called = 1,
    InService = 2,
    Completed = 3,
    Skipped = 4,
    Cancelled = 5
}

public enum QueuePriority
{
    Normal = 0,
    Urgent = 1
}

public sealed class QueueEntryCheckedInDomainEvent : IDomainEvent
{
    public QueueEntryCheckedInDomainEvent(Guid queueEntryId, Guid appointmentId, string queueNumber, DateTime occurredOnUtc)
    {
        QueueEntryId = queueEntryId;
        AppointmentId = appointmentId;
        QueueNumber = queueNumber;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid QueueEntryId { get; }
    public Guid AppointmentId { get; }
    public string QueueNumber { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class QueueEntryCalledDomainEvent : IDomainEvent
{
    public QueueEntryCalledDomainEvent(Guid queueEntryId, DateTime occurredOnUtc)
    {
        QueueEntryId = queueEntryId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid QueueEntryId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class QueueServiceStartedDomainEvent : IDomainEvent
{
    public QueueServiceStartedDomainEvent(Guid queueEntryId, DateTime occurredOnUtc)
    {
        QueueEntryId = queueEntryId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid QueueEntryId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class QueueEntryCompletedDomainEvent : IDomainEvent
{
    public QueueEntryCompletedDomainEvent(Guid queueEntryId, DateTime occurredOnUtc)
    {
        QueueEntryId = queueEntryId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid QueueEntryId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class QueueEntrySkippedDomainEvent : IDomainEvent
{
    public QueueEntrySkippedDomainEvent(Guid queueEntryId, DateTime occurredOnUtc)
    {
        QueueEntryId = queueEntryId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid QueueEntryId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class QueueEntryCancelledDomainEvent : IDomainEvent
{
    public QueueEntryCancelledDomainEvent(Guid queueEntryId, DateTime occurredOnUtc)
    {
        QueueEntryId = queueEntryId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid QueueEntryId { get; }
    public DateTime OccurredOnUtc { get; }
}

/// <summary>
/// Queue entry for day-of clinic flow. Completed here means left the queue/service stage —
/// Medical Visits owns clinical completion.
/// </summary>
public sealed class QueueEntry : AggregateRoot
{
    private QueueEntry()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public string QueueNumber { get; private set; } = string.Empty;
    public Guid PatientId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid ClinicId { get; private set; }
    public DateOnly QueueDate { get; private set; }
    public QueuePriority Priority { get; private set; }
    public QueueStatus Status { get; private set; }
    public DateTime CheckInTimeUtc { get; private set; }
    public DateTime? CalledTimeUtc { get; private set; }
    public DateTime? ServiceStartTimeUtc { get; private set; }
    public DateTime? ServiceEndTimeUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public bool IsActive => Status is QueueStatus.Waiting or QueueStatus.Called or QueueStatus.InService;

    public static QueueEntry CheckIn(
        Guid organizationId,
        Guid branchId,
        string queueNumber,
        Guid patientId,
        Guid appointmentId,
        Guid doctorId,
        Guid clinicId,
        DateOnly queueDate,
        QueuePriority priority,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueNumber);

        var entry = new QueueEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            QueueNumber = queueNumber.Trim(),
            PatientId = patientId,
            AppointmentId = appointmentId,
            DoctorId = doctorId,
            ClinicId = clinicId,
            QueueDate = queueDate,
            Priority = priority,
            Status = QueueStatus.Waiting,
            CheckInTimeUtc = utcNow
        };
        entry.SetCreated(createdBy, utcNow);
        entry.RaiseDomainEvent(new QueueEntryCheckedInDomainEvent(entry.Id, appointmentId, entry.QueueNumber, utcNow));
        return entry;
    }

    public void Call(string? updatedBy, DateTime utcNow)
    {
        EnsureTransition(QueueStatus.Called);
        Status = QueueStatus.Called;
        CalledTimeUtc = utcNow;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new QueueEntryCalledDomainEvent(Id, utcNow));
    }

    public void StartService(string? updatedBy, DateTime utcNow)
    {
        EnsureTransition(QueueStatus.InService);
        Status = QueueStatus.InService;
        ServiceStartTimeUtc = utcNow;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new QueueServiceStartedDomainEvent(Id, utcNow));
    }

    public void Complete(string? updatedBy, DateTime utcNow)
    {
        EnsureTransition(QueueStatus.Completed);
        Status = QueueStatus.Completed;
        ServiceEndTimeUtc = utcNow;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new QueueEntryCompletedDomainEvent(Id, utcNow));
    }

    public void Skip(string? updatedBy, DateTime utcNow)
    {
        EnsureTransition(QueueStatus.Skipped);
        Status = QueueStatus.Skipped;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new QueueEntrySkippedDomainEvent(Id, utcNow));
    }

    public void Cancel(string? updatedBy, DateTime utcNow)
    {
        EnsureTransition(QueueStatus.Cancelled);
        Status = QueueStatus.Cancelled;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new QueueEntryCancelledDomainEvent(Id, utcNow));
    }

    private void EnsureTransition(QueueStatus target)
    {
        var allowed = Status switch
        {
            QueueStatus.Waiting => target is QueueStatus.Called or QueueStatus.Skipped or QueueStatus.Cancelled,
            QueueStatus.Called => target is QueueStatus.InService or QueueStatus.Skipped or QueueStatus.Cancelled,
            QueueStatus.InService => target is QueueStatus.Completed or QueueStatus.Cancelled,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException($"Cannot transition from {Status} to {target}.");
    }
}
