using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Doctors.Application.Clinics;
using ErpClink.Modules.Doctors.Application.Clinics.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/clinics")]
public sealed class ClinicsController : ControllerBase
{
    private readonly IClinicService _clinicService;

    public ClinicsController(IClinicService clinicService)
    {
        _clinicService = clinicService;
    }

    [HttpPost]
    [HasPermission(PermissionCodes.ClinicsCreate)]
    [ProducesResponseType(typeof(ClinicDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ClinicDto>> Create(
        [FromBody] CreateClinicRequest request,
        CancellationToken cancellationToken)
    {
        var clinic = await _clinicService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = clinic.Id }, clinic);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.ClinicsView)]
    [ProducesResponseType(typeof(ClinicDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClinicDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var clinic = await _clinicService.GetByIdAsync(id, cancellationToken);
        return clinic is null ? NotFound() : Ok(clinic);
    }

    [HttpGet("search")]
    [HasPermission(PermissionCodes.ClinicsView)]
    [ProducesResponseType(typeof(PagedClinicsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedClinicsResult>> Search(
        [FromQuery] SearchClinicsRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _clinicService.SearchAsync(request, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.ClinicsUpdate)]
    [ProducesResponseType(typeof(ClinicDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClinicDto>> Update(
        Guid id,
        [FromBody] UpdateClinicRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _clinicService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/activate")]
    [HasPermission(PermissionCodes.ClinicsActivate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _clinicService.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.ClinicsDeactivate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _clinicService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
