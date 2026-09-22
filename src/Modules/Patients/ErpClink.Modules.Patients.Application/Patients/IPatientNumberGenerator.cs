namespace ErpClink.Modules.Patients.Application.Patients;

public interface IPatientNumberGenerator
{
    Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
