using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Patients.Application.Patients;
using ErpClink.Modules.Patients.Application.Patients.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/patients")]
public sealed class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    [HttpPost]
    [HasPermission(PermissionCodes.PatientsCreate)]
    [ProducesResponseType(typeof(RegisterPatientResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterPatientResult>> Register(
        [FromBody] RegisterPatientRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _patientService.RegisterAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Patient.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.PatientsView)]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _patientService.GetByIdAsync(id, cancellationToken);
        return patient is null ? NotFound() : Ok(patient);
    }

    [HttpGet("by-number/{patientNumber}")]
    [HasPermission(PermissionCodes.PatientsView)]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDto>> GetByNumber(string patientNumber, CancellationToken cancellationToken)
    {
        var patient = await _patientService.GetByPatientNumberAsync(patientNumber, cancellationToken);
        return patient is null ? NotFound() : Ok(patient);
    }

    [HttpGet("search")]
    [HasPermission(PermissionCodes.PatientsView)]
    [ProducesResponseType(typeof(PagedPatientsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedPatientsResult>> Search(
        [FromQuery] SearchPatientsRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _patientService.SearchAsync(request, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.PatientsUpdate)]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PatientDto>> Update(
        Guid id,
        [FromBody] UpdatePatientRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _patientService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/activate")]
    [HasPermission(PermissionCodes.PatientsActivate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _patientService.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.PatientsDeactivate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _patientService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/allergies")]
    [HasPermission(PermissionCodes.PatientsAllergiesView)]
    [ProducesResponseType(typeof(IReadOnlyList<AllergyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AllergyDto>>> GetAllergies(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _patientService.GetAllergiesAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/allergies")]
    [HasPermission(PermissionCodes.PatientsAllergiesManage)]
    [ProducesResponseType(typeof(AllergyDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AllergyDto>> AddAllergy(
        Guid id,
        [FromBody] AddAllergyRequest request,
        CancellationToken cancellationToken)
    {
        var allergy = await _patientService.AddAllergyAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetAllergies), new { id }, allergy);
    }

    [HttpPut("{id:guid}/allergies/{allergyId:guid}")]
    [HasPermission(PermissionCodes.PatientsAllergiesManage)]
    [ProducesResponseType(typeof(AllergyDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AllergyDto>> UpdateAllergy(
        Guid id,
        Guid allergyId,
        [FromBody] UpdateAllergyRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _patientService.UpdateAllergyAsync(id, allergyId, request, cancellationToken));
    }

    [HttpPost("{id:guid}/allergies/{allergyId:guid}/deactivate")]
    [HasPermission(PermissionCodes.PatientsAllergiesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeactivateAllergy(Guid id, Guid allergyId, CancellationToken cancellationToken)
    {
        await _patientService.DeactivateAllergyAsync(id, allergyId, cancellationToken);
        return NoContent();
    }
}
