using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Patients.Domain.Patients;

public sealed class PatientDocument : AuditableEntity
{
    private PatientDocument()
    {
    }

    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }

    /// <summary>Optional future link to a medical visit (no FK across modules).</summary>
    public Guid? MedicalVisitId { get; private set; }

    public PatientDocumentCategory Category { get; private set; }
    public PatientDocumentType DocumentType { get; private set; }

    public string OriginalFileName { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public string FileExtension { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }

    public DateOnly? DocumentDate { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static PatientDocument Create(
        Guid id,
        Guid patientId,
        Guid organizationId,
        Guid branchId,
        PatientDocumentCategory category,
        PatientDocumentType documentType,
        string originalFileName,
        string storageKey,
        string fileExtension,
        string contentType,
        long fileSizeBytes,
        DateOnly? documentDate,
        string? description,
        Guid? medicalVisitId,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Document id is required.", nameof(id));
        }
        if (fileSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes));
        }

        if (!PatientDocumentTypeRules.BelongsToCategory(documentType, category))
        {
            throw new InvalidOperationException("Document type does not belong to the selected category.");
        }

        var document = new PatientDocument
        {
            Id = id,
            PatientId = patientId,
            OrganizationId = organizationId,
            BranchId = branchId,
            MedicalVisitId = medicalVisitId,
            Category = category,
            DocumentType = documentType,
            OriginalFileName = originalFileName.Trim(),
            StorageKey = storageKey.Trim(),
            FileExtension = fileExtension.Trim().TrimStart('.').ToLowerInvariant(),
            ContentType = contentType.Trim(),
            FileSizeBytes = fileSizeBytes,
            DocumentDate = documentDate,
            Description = NormalizeOptional(description),
            IsActive = true
        };

        document.SetCreated(createdBy, utcNow);
        return document;
    }

    public void UpdateMetadata(
        PatientDocumentCategory category,
        PatientDocumentType documentType,
        DateOnly? documentDate,
        string? description,
        Guid? medicalVisitId,
        string? updatedBy,
        DateTime utcNow)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Cannot update an inactive document.");
        }

        if (!PatientDocumentTypeRules.BelongsToCategory(documentType, category))
        {
            throw new InvalidOperationException("Document type does not belong to the selected category.");
        }

        Category = category;
        DocumentType = documentType;
        DocumentDate = documentDate;
        Description = NormalizeOptional(description);
        MedicalVisitId = medicalVisitId;
        SetUpdated(updatedBy, utcNow);
    }

    public void Deactivate(string? updatedBy, DateTime utcNow)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        SetUpdated(updatedBy, utcNow);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
