namespace ErpClink.Modules.Appointments.Application.Contracts;

public interface IAppointmentLookup
{
    Task<AppointmentLookupResult?> GetAsync(Guid appointmentId, CancellationToken cancellationToken = default);
}

public interface IAppointmentCheckInPort
{
    /// <summary>
    /// Marks appointment CheckedIn. Appointment module owns the state transition.
    /// </summary>
    Task CheckInAsync(Guid appointmentId, CancellationToken cancellationToken = default);
}

public sealed record AppointmentLookupResult(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    string AppointmentNumber,
    Guid PatientId,
    Guid DoctorId,
    Guid ClinicId,
    DateOnly AppointmentDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Status);
