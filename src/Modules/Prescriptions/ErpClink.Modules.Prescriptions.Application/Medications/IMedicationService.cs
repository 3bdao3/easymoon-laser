using ErpClink.Modules.Prescriptions.Application.Medications.Models;

namespace ErpClink.Modules.Prescriptions.Application.Medications;

public interface IMedicationService
{
    Task<MedicationDto> CreateAsync(CreateMedicationRequest request, CancellationToken cancellationToken = default);
    Task<MedicationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedMedicationsResult> SearchAsync(SearchMedicationsRequest request, CancellationToken cancellationToken = default);
    Task<MedicationDto> UpdateAsync(Guid id, UpdateMedicationRequest request, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
