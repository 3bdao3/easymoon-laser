using ErpClink.Modules.Patients.Application.Patients.Models;

namespace ErpClink.Modules.Patients.Application.Patients;

public interface IPatientDocumentService
{
    Task<PagedPatientDocumentsResult> SearchAsync(
        Guid patientId,
        SearchPatientDocumentsRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientDocumentDto?> GetByIdAsync(
        Guid patientId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<PatientDocumentSummaryDto> GetSummaryAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<UploadPatientDocumentResult> UploadAsync(
        Guid patientId,
        UploadPatientDocumentCommand command,
        CancellationToken cancellationToken = default);

    Task<PatientDocumentDto> UpdateMetadataAsync(
        Guid patientId,
        Guid documentId,
        UpdatePatientDocumentMetadataRequest request,
        CancellationToken cancellationToken = default);

    Task DeactivateAsync(
        Guid patientId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<(Stream Content, string ContentType, string DownloadFileName)> OpenDownloadAsync(
        Guid patientId,
        Guid documentId,
        CancellationToken cancellationToken = default);
}
