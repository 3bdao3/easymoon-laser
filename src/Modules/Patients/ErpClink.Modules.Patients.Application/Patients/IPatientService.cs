using ErpClink.Modules.Patients.Application.Patients.Models;

namespace ErpClink.Modules.Patients.Application.Patients;

public interface IPatientService
{
    Task<RegisterPatientResult> RegisterAsync(RegisterPatientRequest request, CancellationToken cancellationToken = default);
    Task<PatientDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PatientDto?> GetByPatientNumberAsync(string patientNumber, CancellationToken cancellationToken = default);
    Task<PagedPatientsResult> SearchAsync(SearchPatientsRequest request, CancellationToken cancellationToken = default);
    Task<PatientDto> UpdateAsync(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AllergyDto>> GetAllergiesAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<AllergyDto> AddAllergyAsync(Guid patientId, AddAllergyRequest request, CancellationToken cancellationToken = default);
    Task<AllergyDto> UpdateAllergyAsync(Guid patientId, Guid allergyId, UpdateAllergyRequest request, CancellationToken cancellationToken = default);
    Task DeactivateAllergyAsync(Guid patientId, Guid allergyId, CancellationToken cancellationToken = default);
}
