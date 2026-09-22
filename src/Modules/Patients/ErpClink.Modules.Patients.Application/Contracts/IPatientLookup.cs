namespace ErpClink.Modules.Patients.Application.Contracts;

public interface IPatientLookup
{
    Task<PatientLookupResult?> GetAsync(Guid patientId, CancellationToken cancellationToken = default);
}

public sealed record PatientLookupResult(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    bool IsActive,
    string PatientNumber);
