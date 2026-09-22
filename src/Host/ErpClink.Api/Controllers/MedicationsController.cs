using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Prescriptions.Application.Medications;
using ErpClink.Modules.Prescriptions.Application.Medications.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/medications")]
public sealed class MedicationsController : ControllerBase
{
    private readonly IMedicationService _medications;

    public MedicationsController(IMedicationService medications) => _medications = medications;

    [HttpPost]
    [HasPermission(PermissionCodes.MedicationsCreate)]
    [ProducesResponseType(typeof(MedicationDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<MedicationDto>> Create(
        [FromBody] CreateMedicationRequest request,
        CancellationToken cancellationToken)
    {
        var med = await _medications.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = med.Id }, med);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.MedicationsView)]
    public async Task<ActionResult<MedicationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var med = await _medications.GetByIdAsync(id, cancellationToken);
        return med is null ? NotFound() : Ok(med);
    }

    [HttpGet("search")]
    [HasPermission(PermissionCodes.MedicationsView)]
    public async Task<ActionResult<PagedMedicationsResult>> Search(
        [FromQuery] SearchMedicationsRequest request,
        CancellationToken cancellationToken)
        => Ok(await _medications.SearchAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.MedicationsUpdate)]
    public async Task<ActionResult<MedicationDto>> Update(
        Guid id,
        [FromBody] UpdateMedicationRequest request,
        CancellationToken cancellationToken)
        => Ok(await _medications.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [HasPermission(PermissionCodes.MedicationsActivate)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _medications.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.MedicationsDeactivate)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _medications.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
