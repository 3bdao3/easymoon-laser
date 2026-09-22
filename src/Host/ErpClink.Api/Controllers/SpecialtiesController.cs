using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Doctors.Application.Specialties;
using ErpClink.Modules.Doctors.Application.Specialties.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/specialties")]
public sealed class SpecialtiesController : ControllerBase
{
    private readonly ISpecialtyService _specialtyService;

    public SpecialtiesController(ISpecialtyService specialtyService)
    {
        _specialtyService = specialtyService;
    }

    [HttpGet]
    [HasPermission(PermissionCodes.SpecialtiesView)]
    [ProducesResponseType(typeof(IReadOnlyList<SpecialtyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SpecialtyDto>>> List(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        return Ok(await _specialtyService.ListAsync(isActive, cancellationToken));
    }

    [HttpPost]
    [HasPermission(PermissionCodes.SpecialtiesManage)]
    [ProducesResponseType(typeof(SpecialtyDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SpecialtyDto>> Create(
        [FromBody] CreateSpecialtyRequest request,
        CancellationToken cancellationToken)
    {
        var specialty = await _specialtyService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(List), null, specialty);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.SpecialtiesManage)]
    [ProducesResponseType(typeof(SpecialtyDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SpecialtyDto>> Update(
        Guid id,
        [FromBody] UpdateSpecialtyRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _specialtyService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/activate")]
    [HasPermission(PermissionCodes.SpecialtiesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _specialtyService.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.SpecialtiesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _specialtyService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
