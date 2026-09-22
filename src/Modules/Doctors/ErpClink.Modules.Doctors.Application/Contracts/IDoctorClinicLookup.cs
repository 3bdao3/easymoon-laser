namespace ErpClink.Modules.Doctors.Application.Contracts;

/// <summary>
/// Read-only port for other modules (e.g. Scheduling) to validate doctor/clinic/assignment
/// without accessing Doctors EF entities.
/// </summary>
public interface IDoctorClinicLookup
{
    Task<DoctorClinicLookupResult?> GetDoctorAsync(Guid doctorId, CancellationToken cancellationToken = default);
    Task<DoctorClinicLookupResult?> GetClinicAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveAssignmentAsync(
        Guid doctorId,
        Guid clinicId,
        DateOnly onDate,
        CancellationToken cancellationToken = default);
}

public sealed record DoctorClinicLookupResult(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    bool IsActive);
