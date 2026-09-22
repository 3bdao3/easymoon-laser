namespace ErpClink.Modules.Queue.Application.Contracts;

/// <summary>
/// Application port for Medical Visits to verify/transition queue state without Infrastructure coupling.
/// </summary>
public interface IQueueVisitPort
{
    Task<QueueVisitLookupResult?> GetByIdAsync(Guid queueEntryId, CancellationToken cancellationToken = default);
    Task<QueueVisitLookupResult?> GetActiveByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions Called → InService when needed for clinical encounter start.
    /// No-op if already InService. Fails for other statuses.
    /// </summary>
    Task EnsureInServiceAsync(Guid queueEntryId, CancellationToken cancellationToken = default);
}

public sealed record QueueVisitLookupResult(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    Guid ClinicId,
    DateOnly QueueDate,
    string Status);
