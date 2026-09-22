using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Doctors.Application.Doctors;
using ErpClink.Modules.Doctors.Application.Doctors.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/doctors")]
public sealed class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;

    public DoctorsController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    [HttpPost]
    [HasPermission(PermissionCodes.DoctorsCreate)]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DoctorDto>> Register(
        [FromBody] RegisterDoctorRequest request,
        CancellationToken cancellationToken)
    {
        var doctor = await _doctorService.RegisterAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = doctor.Id }, doctor);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.DoctorsView)]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var doctor = await _doctorService.GetByIdAsync(id, cancellationToken);
        return doctor is null ? NotFound() : Ok(doctor);
    }

    [HttpGet("by-number/{doctorNumber}")]
    [HasPermission(PermissionCodes.DoctorsView)]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorDto>> GetByNumber(string doctorNumber, CancellationToken cancellationToken)
    {
        var doctor = await _doctorService.GetByNumberAsync(doctorNumber, cancellationToken);
        return doctor is null ? NotFound() : Ok(doctor);
    }

    [HttpGet("search")]
    [HasPermission(PermissionCodes.DoctorsView)]
    [ProducesResponseType(typeof(PagedDoctorsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedDoctorsResult>> Search(
        [FromQuery] SearchDoctorsRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _doctorService.SearchAsync(request, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.DoctorsUpdate)]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DoctorDto>> Update(
        Guid id,
        [FromBody] UpdateDoctorRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _doctorService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/activate")]
    [HasPermission(PermissionCodes.DoctorsActivate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _doctorService.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.DoctorsDeactivate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _doctorService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/clinics")]
    [HasPermission(PermissionCodes.DoctorsView)]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorClinicAssignmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DoctorClinicAssignmentDto>>> GetClinics(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _doctorService.GetClinicAssignmentsAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/clinics")]
    [HasPermission(PermissionCodes.DoctorsAssignClinic)]
    [ProducesResponseType(typeof(DoctorClinicAssignmentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DoctorClinicAssignmentDto>> AssignClinic(
        Guid id,
        [FromBody] AssignDoctorToClinicRequest request,
        CancellationToken cancellationToken)
    {
        var assignment = await _doctorService.AssignToClinicAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetClinics), new { id }, assignment);
    }

    [HttpPost("{id:guid}/clinics/{clinicId:guid}/deactivate")]
    [HasPermission(PermissionCodes.DoctorsAssignClinic)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeactivateClinicAssignment(
        Guid id,
        Guid clinicId,
        CancellationToken cancellationToken)
    {
        await _doctorService.DeactivateClinicAssignmentAsync(id, clinicId, cancellationToken);
        return NoContent();
    }
}
