using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Patients.Application.Patients;
using ErpClink.Modules.Patients.Application.Patients.Models;
using ErpClink.Modules.Patients.Domain.Patients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/patients/{patientId:guid}/documents")]
public sealed class PatientDocumentsController : ControllerBase
{
    private readonly IPatientDocumentService _documents;

    public PatientDocumentsController(IPatientDocumentService documents)
    {
        _documents = documents;
    }

    [HttpGet]
    [HasPermission(PermissionCodes.PatientsDocumentsView)]
    [ProducesResponseType(typeof(PagedPatientDocumentsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedPatientDocumentsResult>> Search(
        Guid patientId,
        [FromQuery] SearchPatientDocumentsRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _documents.SearchAsync(patientId, request, cancellationToken));
    }

    [HttpGet("summary")]
    [HasPermission(PermissionCodes.PatientsDocumentsView)]
    [ProducesResponseType(typeof(PatientDocumentSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PatientDocumentSummaryDto>> Summary(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        return Ok(await _documents.GetSummaryAsync(patientId, cancellationToken));
    }

    [HttpGet("{documentId:guid}")]
    [HasPermission(PermissionCodes.PatientsDocumentsView)]
    [ProducesResponseType(typeof(PatientDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDocumentDto>> GetById(
        Guid patientId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var document = await _documents.GetByIdAsync(patientId, documentId, cancellationToken);
        return document is null ? NotFound() : Ok(document);
    }

    [HttpGet("{documentId:guid}/download")]
    [HasPermission(PermissionCodes.PatientsDocumentsView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        Guid patientId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var (content, contentType, fileName) =
            await _documents.OpenDownloadAsync(patientId, documentId, cancellationToken);
        return File(content, contentType, fileName);
    }

    [HttpPost]
    [HasPermission(PermissionCodes.PatientsDocumentsManage)]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 12 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadPatientDocumentResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UploadPatientDocumentResult>> Upload(
        Guid patientId,
        IFormFile file,
        [FromForm] PatientDocumentCategory category,
        [FromForm] PatientDocumentType documentType,
        [FromForm] DateOnly? documentDate,
        [FromForm] string? description,
        [FromForm] Guid? medicalVisitId,
        [FromForm] bool allowPossibleDuplicate = false,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { code = "patients.document_empty", detail = "Uploaded file is empty." });
        }

        await using var stream = file.OpenReadStream();
        var command = new UploadPatientDocumentCommand
        {
            Content = stream,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType ?? "application/octet-stream",
            FileSizeBytes = file.Length,
            Category = category,
            DocumentType = documentType,
            DocumentDate = documentDate,
            Description = description,
            MedicalVisitId = medicalVisitId,
            AllowPossibleDuplicate = allowPossibleDuplicate
        };

        var result = await _documents.UploadAsync(patientId, command, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { patientId, documentId = result.Document.Id },
            result);
    }

    [HttpPut("{documentId:guid}")]
    [HasPermission(PermissionCodes.PatientsDocumentsManage)]
    [ProducesResponseType(typeof(PatientDocumentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PatientDocumentDto>> UpdateMetadata(
        Guid patientId,
        Guid documentId,
        [FromBody] UpdatePatientDocumentMetadataRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _documents.UpdateMetadataAsync(patientId, documentId, request, cancellationToken));
    }

    [HttpPost("{documentId:guid}/deactivate")]
    [HasPermission(PermissionCodes.PatientsDocumentsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(
        Guid patientId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        await _documents.DeactivateAsync(patientId, documentId, cancellationToken);
        return NoContent();
    }
}
