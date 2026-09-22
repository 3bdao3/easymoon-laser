using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.Application.Options;
using ErpClink.Modules.Patients.Application.Patients;
using ErpClink.Modules.Patients.Application.Patients.Models;
using ErpClink.Modules.Patients.Domain.Patients;
using ErpClink.Modules.Patients.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ErpClink.Modules.Patients.Infrastructure.Patients;

public sealed class PatientDocumentService : IPatientDocumentService
{
    private static readonly IReadOnlyDictionary<string, string[]> ExtensionContentTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["pdf"] = ["application/pdf"],
            ["doc"] = ["application/msword"],
            ["docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            ["jpg"] = ["image/jpeg"],
            ["jpeg"] = ["image/jpeg"],
            ["png"] = ["image/png"],
            ["webp"] = ["image/webp"]
        };

    private readonly PatientsDbContext _dbContext;
    private readonly IFileStorage _fileStorage;
    private readonly IOrganizationContext _organizationContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly FileStorageOptions _storageOptions;
    private readonly IValidator<UpdatePatientDocumentMetadataRequest> _updateValidator;
    private readonly IValidator<SearchPatientDocumentsRequest> _searchValidator;

    public PatientDocumentService(
        PatientsDbContext dbContext,
        IFileStorage fileStorage,
        IOrganizationContext organizationContext,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IOptions<FileStorageOptions> storageOptions,
        IValidator<UpdatePatientDocumentMetadataRequest> updateValidator,
        IValidator<SearchPatientDocumentsRequest> searchValidator)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _organizationContext = organizationContext;
        _currentUser = currentUser;
        _clock = clock;
        _storageOptions = storageOptions.Value;
        _updateValidator = updateValidator;
        _searchValidator = searchValidator;
    }

    public async Task<PagedPatientDocumentsResult> SearchAsync(
        Guid patientId,
        SearchPatientDocumentsRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_searchValidator, request, cancellationToken);
        _ = await GetRequiredPatientAsync(patientId, cancellationToken);

        var paging = new PagedRequest(request.Page, request.PageSize);
        var query = DocumentsForPatient(patientId);

        if (request.IsActive.HasValue)
        {
            query = query.Where(d => d.IsActive == request.IsActive.Value);
        }

        if (request.Category.HasValue)
        {
            query = query.Where(d => d.Category == request.Category.Value);
        }

        if (request.DocumentType.HasValue)
        {
            query = query.Where(d => d.DocumentType == request.DocumentType.Value);
        }

        if (request.DateFrom.HasValue)
        {
            query = query.Where(d => d.DocumentDate >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(d => d.DocumentDate <= request.DateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.UploadedBy))
        {
            var uploadedBy = request.UploadedBy.Trim();
            query = query.Where(d => d.CreatedBy == uploadedBy);
        }

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.Trim();
            query = query.Where(d =>
                d.OriginalFileName.Contains(q) ||
                (d.Description != null && d.Description.Contains(q)));
        }

        var total = await query.CountAsync(cancellationToken);
        var entities = await query
            .OrderByDescending(d => d.DocumentDate ?? DateOnly.FromDateTime(d.CreatedAtUtc))
            .ThenByDescending(d => d.CreatedAtUtc)
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(MapDto).ToList();
        return new PagedPatientDocumentsResult(items, paging.NormalizedPage, paging.NormalizedPageSize, total);
    }

    public async Task<PatientDocumentDto?> GetByIdAsync(
        Guid patientId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        _ = await GetRequiredPatientAsync(patientId, cancellationToken);
        var document = await DocumentsForPatient(patientId)
            .AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        return document is null ? null : MapDto(document);
    }

    public async Task<PatientDocumentSummaryDto> GetSummaryAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        _ = await GetRequiredPatientAsync(patientId, cancellationToken);

        var active = await DocumentsForPatient(patientId)
            .AsNoTracking()
            .Where(d => d.IsActive)
            .GroupBy(d => d.Category)
            .Select(g => new PatientDocumentCategoryCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        return new PatientDocumentSummaryDto(active.Sum(x => x.Count), active);
    }

    public async Task<UploadPatientDocumentResult> UploadAsync(
        Guid patientId,
        UploadPatientDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Content);

        var patient = await GetRequiredPatientAsync(patientId, cancellationToken);
        if (!patient.IsActive)
        {
            throw new AppException("patients.document_upload_failed", "Patient is inactive.", 400);
        }

        if (!PatientDocumentTypeRules.BelongsToCategory(command.DocumentType, command.Category))
        {
            throw new AppException(
                "patients.document_invalid_type",
                "Document type does not belong to the selected category.",
                400);
        }

        var (safeName, extension, contentType) = ValidateFile(
            command.OriginalFileName,
            command.ContentType,
            command.FileSizeBytes);

        var possibleDuplicate = await DocumentsForPatient(patientId)
            .AnyAsync(
                d => d.IsActive &&
                     d.OriginalFileName == safeName &&
                     d.FileSizeBytes == command.FileSizeBytes,
                cancellationToken);

        if (possibleDuplicate && !command.AllowPossibleDuplicate)
        {
            throw new AppException(
                "patients.document_possible_duplicate",
                "A similar active document already exists for this patient.",
                409);
        }

        var documentId = Guid.NewGuid();
        var storageKey =
            $"{patient.OrganizationId:N}/{patient.Id:N}/{documentId:N}.{extension}";

        // Persist bytes first; then metadata. If DB fails, attempt storage cleanup.
        await _fileStorage.SaveAsync(storageKey, command.Content, contentType, cancellationToken);

        PatientDocument document;
        try
        {
            document = PatientDocument.Create(
                documentId,
                patient.Id,
                patient.OrganizationId,
                patient.BranchId,
                command.Category,
                command.DocumentType,
                safeName,
                storageKey,
                extension,
                contentType,
                command.FileSizeBytes,
                command.DocumentDate,
                command.Description,
                command.MedicalVisitId,
                _currentUser.UserId,
                _clock.UtcNow);

            _dbContext.PatientDocuments.Add(document);
            patient.Touch(_currentUser.UserId, _clock.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            try
            {
                await _fileStorage.DeleteAsync(storageKey, cancellationToken);
            }
            catch
            {
                // Best-effort cleanup
            }

            throw;
        }

        return new UploadPatientDocumentResult(MapDto(document), possibleDuplicate);
    }

    public async Task<PatientDocumentDto> UpdateMetadataAsync(
        Guid patientId,
        Guid documentId,
        UpdatePatientDocumentMetadataRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);
        var patient = await GetRequiredPatientAsync(patientId, cancellationToken);
        if (!patient.IsActive)
        {
            throw new AppException("patients.document_update_failed", "Patient is inactive.", 400);
        }

        var document = await DocumentsForPatient(patientId)
            .SingleOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null)
        {
            throw new AppException("patients.document_not_found", "Document was not found for this patient.", 404);
        }

        try
        {
            document.UpdateMetadata(
                request.Category,
                request.DocumentType,
                request.DocumentDate,
                request.Description,
                request.MedicalVisitId,
                _currentUser.UserId,
                _clock.UtcNow);

            patient.Touch(_currentUser.UserId, _clock.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapDto(document);
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException("patients.document_update_failed", ex.Message, 400);
        }
    }

    public async Task DeactivateAsync(
        Guid patientId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var patient = await GetRequiredPatientAsync(patientId, cancellationToken);
        var document = await DocumentsForPatient(patientId)
            .SingleOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null)
        {
            throw new AppException("patients.document_not_found", "Document was not found for this patient.", 404);
        }

        document.Deactivate(_currentUser.UserId, _clock.UtcNow);
        patient.Touch(_currentUser.UserId, _clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        // Soft-delete only — retain storage for audit/recovery.
    }

    public async Task<(Stream Content, string ContentType, string DownloadFileName)> OpenDownloadAsync(
        Guid patientId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        _ = await GetRequiredPatientAsync(patientId, cancellationToken);
        var document = await DocumentsForPatient(patientId)
            .AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null || !document.IsActive)
        {
            throw new AppException("patients.document_not_found", "Document was not found for this patient.", 404);
        }

        var stream = await _fileStorage.OpenReadAsync(document.StorageKey, cancellationToken);
        return (stream, document.ContentType, document.OriginalFileName);
    }

    private IQueryable<PatientDocument> DocumentsForPatient(Guid patientId) =>
        _dbContext.PatientDocuments.Where(d =>
            d.PatientId == patientId &&
            d.OrganizationId == _organizationContext.OrganizationId);

    private async Task<Patient> GetRequiredPatientAsync(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _dbContext.Patients
            .SingleOrDefaultAsync(
                p => p.Id == id && p.OrganizationId == _organizationContext.OrganizationId,
                cancellationToken);

        if (patient is null)
        {
            throw new AppException("patients.not_found", "Patient was not found.", 404);
        }

        return patient;
    }

    private (string SafeName, string Extension, string ContentType) ValidateFile(
        string originalFileName,
        string contentType,
        long fileSizeBytes)
    {
        if (fileSizeBytes <= 0)
        {
            throw new AppException("patients.document_empty", "Uploaded file is empty.", 400);
        }

        if (fileSizeBytes > _storageOptions.MaxFileSizeBytes)
        {
            throw new AppException(
                "patients.document_too_large",
                $"File exceeds the maximum allowed size of {_storageOptions.MaxFileSizeBytes} bytes.",
                400);
        }

        var safeName = Path.GetFileName(originalFileName?.Trim() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            throw new AppException("patients.document_invalid_name", "File name is required.", 400);
        }

        if (safeName.Length > 255)
        {
            throw new AppException("patients.document_invalid_name", "File name is too long.", 400);
        }

        var extension = Path.GetExtension(safeName).TrimStart('.').ToLowerInvariant();
        var allowed = _storageOptions.AllowedExtensions
            .Select(e => e.Trim().TrimStart('.').ToLowerInvariant())
            .Where(e => e.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!allowed.Contains(extension) || !ExtensionContentTypes.ContainsKey(extension))
        {
            throw new AppException(
                "patients.document_unsupported_type",
                "File type is not allowed.",
                400);
        }

        var normalizedContentType = string.IsNullOrWhiteSpace(contentType)
            ? ExtensionContentTypes[extension][0]
            : contentType.Split(';')[0].Trim();

        var allowedTypes = ExtensionContentTypes[extension];
        if (!allowedTypes.Contains(normalizedContentType, StringComparer.OrdinalIgnoreCase)
            && !string.Equals(normalizedContentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(
                "patients.document_unsupported_type",
                "Content type does not match the file extension.",
                400);
        }

        if (string.Equals(normalizedContentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            normalizedContentType = allowedTypes[0];
        }

        return (safeName, extension, normalizedContentType);
    }

    private static PatientDocumentDto MapDto(PatientDocument d) =>
        new(
            d.Id,
            d.PatientId,
            d.MedicalVisitId,
            d.Category,
            d.DocumentType,
            d.OriginalFileName,
            d.FileExtension,
            d.ContentType,
            d.FileSizeBytes,
            d.DocumentDate,
            d.Description,
            d.IsActive,
            d.CreatedAtUtc,
            d.CreatedBy,
            d.UpdatedAtUtc,
            d.UpdatedBy);

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            var message = string.Join(" ", result.Errors.Select(e => e.ErrorMessage));
            throw new AppException("validation.failed", message, 400);
        }
    }
}
