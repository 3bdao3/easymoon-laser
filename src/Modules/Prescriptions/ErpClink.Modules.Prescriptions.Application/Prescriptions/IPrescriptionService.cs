using ErpClink.Modules.Prescriptions.Application.Prescriptions.Models;

namespace ErpClink.Modules.Prescriptions.Application.Prescriptions;

public interface IPrescriptionService
{
    Task<PrescriptionDto> CreateAsync(CreatePrescriptionRequest request, CancellationToken cancellationToken = default);
    Task<PrescriptionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PrescriptionDto?> GetByNumberAsync(string prescriptionNumber, CancellationToken cancellationToken = default);
    Task<PagedPrescriptionsResult> SearchAsync(SearchPrescriptionsRequest request, CancellationToken cancellationToken = default);
    Task<PagedPrescriptionsResult> GetPatientHistoryAsync(Guid patientId, PatientPrescriptionHistoryRequest request, CancellationToken cancellationToken = default);
    Task<PrescriptionDto> UpdateAsync(Guid id, UpdatePrescriptionRequest request, CancellationToken cancellationToken = default);
    Task<PrescriptionDto> AddItemAsync(Guid id, PrescriptionItemInput request, CancellationToken cancellationToken = default);
    Task<PrescriptionDto> UpdateItemAsync(Guid id, Guid itemId, UpdatePrescriptionItemRequest request, CancellationToken cancellationToken = default);
    Task<PrescriptionDto> RemoveItemAsync(Guid id, Guid itemId, byte[]? rowVersion, CancellationToken cancellationToken = default);
    Task IssueAsync(Guid id, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid id, CancelPrescriptionRequest request, CancellationToken cancellationToken = default);
}

public interface IPrescriptionNumberGenerator
{
    Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
