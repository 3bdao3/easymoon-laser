namespace ErpClink.Modules.MedicalVisits.Application.Contracts;

/// <summary>
/// Application port for Billing to validate Medical Visit context without Infrastructure coupling.
/// </summary>
public interface IMedicalVisitBillingPort
{
    Task<MedicalVisitBillingLookup?> GetAsync(Guid medicalVisitId, CancellationToken cancellationToken = default);
}

public sealed record MedicalVisitBillingLookup(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    Guid PatientId,
    DateOnly VisitDate,
    string Status);
