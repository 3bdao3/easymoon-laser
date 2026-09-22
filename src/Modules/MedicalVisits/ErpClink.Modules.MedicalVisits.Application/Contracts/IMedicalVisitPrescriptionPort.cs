namespace ErpClink.Modules.MedicalVisits.Application.Contracts;

/// <summary>
/// Application port for Prescriptions to validate Medical Visit context without Infrastructure coupling.
/// </summary>
public interface IMedicalVisitPrescriptionPort
{
    Task<MedicalVisitPrescriptionLookup?> GetAsync(Guid medicalVisitId, CancellationToken cancellationToken = default);
}

public sealed record MedicalVisitPrescriptionLookup(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    Guid PatientId,
    Guid DoctorId,
    Guid ClinicId,
    DateOnly VisitDate,
    string Status);
