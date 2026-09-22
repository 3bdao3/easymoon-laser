using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.MedicalVisits.Application.Visits;
using ErpClink.Modules.MedicalVisits.Application.Visits.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/medical-visits")]
public sealed class MedicalVisitsController : ControllerBase
{
    private readonly IMedicalVisitService _visits;

    public MedicalVisitsController(IMedicalVisitService visits) => _visits = visits;

    [HttpPost("start")]
    [HasPermission(PermissionCodes.MedicalVisitsCreate)]
    [ProducesResponseType(typeof(MedicalVisitDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<MedicalVisitDto>> Start(
        [FromBody] StartMedicalVisitRequest request,
        CancellationToken cancellationToken)
    {
        var visit = await _visits.StartAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = visit.Id }, visit);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.MedicalVisitsView)]
    [ProducesResponseType(typeof(MedicalVisitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicalVisitDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var visit = await _visits.GetByIdAsync(id, cancellationToken);
        return visit is null ? NotFound() : Ok(visit);
    }

    [HttpGet("by-number/{visitNumber}")]
    [HasPermission(PermissionCodes.MedicalVisitsView)]
    [ProducesResponseType(typeof(MedicalVisitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicalVisitDto>> GetByNumber(string visitNumber, CancellationToken cancellationToken)
    {
        var visit = await _visits.GetByNumberAsync(visitNumber, cancellationToken);
        return visit is null ? NotFound() : Ok(visit);
    }

    [HttpGet("patients/{patientId:guid}/history")]
    [HasPermission(PermissionCodes.MedicalVisitsView)]
    [ProducesResponseType(typeof(PagedMedicalVisitsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedMedicalVisitsResult>> PatientHistory(
        Guid patientId,
        [FromQuery] PatientHistoryRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _visits.GetPatientHistoryAsync(patientId, request, cancellationToken));
    }

    [HttpGet("patients/{patientId:guid}/context")]
    [HasPermission(PermissionCodes.MedicalVisitsView)]
    [ProducesResponseType(typeof(PatientVisitContextDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PatientVisitContextDto>> PatientVisitContext(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        return Ok(await _visits.GetPatientVisitContextAsync(patientId, cancellationToken));
    }

    [HttpGet("search")]
    [HasPermission(PermissionCodes.MedicalVisitsView)]
    [ProducesResponseType(typeof(PagedMedicalVisitsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedMedicalVisitsResult>> Search(
        [FromQuery] SearchMedicalVisitsRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _visits.SearchAsync(request, cancellationToken));
    }

    [HttpPut("{id:guid}/clinical-notes")]
    [HasPermission(PermissionCodes.MedicalVisitsUpdate)]
    [ProducesResponseType(typeof(MedicalVisitDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MedicalVisitDto>> UpdateClinicalNotes(
        Guid id,
        [FromBody] UpdateClinicalNotesRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _visits.UpdateClinicalNotesAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/complete")]
    [HasPermission(PermissionCodes.MedicalVisitsComplete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        await _visits.CompleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(PermissionCodes.MedicalVisitsCancel)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelMedicalVisitRequest? request,
        CancellationToken cancellationToken)
    {
        await _visits.CancelAsync(id, request ?? new CancelMedicalVisitRequest(null), cancellationToken);
        return NoContent();
    }
}
