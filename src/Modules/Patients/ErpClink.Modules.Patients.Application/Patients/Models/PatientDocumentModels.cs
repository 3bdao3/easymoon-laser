using ErpClink.Modules.Patients.Domain.Patients;

namespace ErpClink.Modules.Patients.Application.Patients.Models;

public sealed record SearchPatientDocumentsRequest(
    string? Query = null,
    PatientDocumentCategory? Category = null,
    PatientDocumentType? DocumentType = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    bool? IsActive = true,
    string? UploadedBy = null,
    int Page = 1,
    int PageSize = 20);

public sealed record UpdatePatientDocumentMetadataRequest(
    PatientDocumentCategory Category,
    PatientDocumentType DocumentType,
    DateOnly? DocumentDate,
    string? Description,
    Guid? MedicalVisitId);

public sealed record PatientDocumentDto(
    Guid Id,
    Guid PatientId,
    Guid? MedicalVisitId,
    PatientDocumentCategory Category,
    PatientDocumentType DocumentType,
    string OriginalFileName,
    string FileExtension,
    string ContentType,
    long FileSizeBytes,
    DateOnly? DocumentDate,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    DateTime? UpdatedAtUtc,
    string? UpdatedBy);

public sealed record UploadPatientDocumentResult(
    PatientDocumentDto Document,
    bool PossibleDuplicateWarning);

public sealed record PagedPatientDocumentsResult(
    IReadOnlyList<PatientDocumentDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record PatientDocumentCategoryCountDto(
    PatientDocumentCategory Category,
    int Count);

public sealed record PatientDocumentSummaryDto(
    int TotalActive,
    IReadOnlyList<PatientDocumentCategoryCountDto> ByCategory);

public sealed class UploadPatientDocumentCommand
{
    public required Stream Content { get; init; }
    public required string OriginalFileName { get; init; }
    public required string ContentType { get; init; }
    public required long FileSizeBytes { get; init; }
    public required PatientDocumentCategory Category { get; init; }
    public required PatientDocumentType DocumentType { get; init; }
    public DateOnly? DocumentDate { get; init; }
    public string? Description { get; init; }
    public Guid? MedicalVisitId { get; init; }
    public bool AllowPossibleDuplicate { get; init; }
}
