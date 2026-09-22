using ErpClink.Modules.MedicalVisits.Application.Visits.Models;

namespace ErpClink.Modules.MedicalVisits.Application.Visits;

public interface IMedicalVisitService
{
    Task<MedicalVisitDto> StartAsync(StartMedicalVisitRequest request, CancellationToken cancellationToken = default);
    Task<MedicalVisitDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MedicalVisitDto?> GetByNumberAsync(string visitNumber, CancellationToken cancellationToken = default);
    Task<PagedMedicalVisitsResult> GetPatientHistoryAsync(Guid patientId, PatientHistoryRequest request, CancellationToken cancellationToken = default);
    Task<PatientVisitContextDto> GetPatientVisitContextAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<PagedMedicalVisitsResult> SearchAsync(SearchMedicalVisitsRequest request, CancellationToken cancellationToken = default);
    Task<MedicalVisitDto> UpdateClinicalNotesAsync(Guid id, UpdateClinicalNotesRequest request, CancellationToken cancellationToken = default);
    Task CompleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid id, CancelMedicalVisitRequest request, CancellationToken cancellationToken = default);
}

public interface IVisitNumberGenerator
{
    Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
