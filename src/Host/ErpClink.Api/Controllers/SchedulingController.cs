using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Scheduling.Application.Schedules;
using ErpClink.Modules.Scheduling.Application.Schedules.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/scheduling")]
public sealed class SchedulingController : ControllerBase
{
    private readonly IDoctorScheduleService _schedules;
    private readonly IScheduleAvailabilityService _availability;

    public SchedulingController(
        IDoctorScheduleService schedules,
        IScheduleAvailabilityService availability)
    {
        _schedules = schedules;
        _availability = availability;
    }

    [HttpPost("schedules")]
    [HasPermission(PermissionCodes.SchedulingCreate)]
    [ProducesResponseType(typeof(DoctorScheduleDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DoctorScheduleDto>> Create(
        [FromBody] CreateDoctorScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var schedule = await _schedules.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = schedule.Id }, schedule);
    }

    [HttpGet("schedules/{id:guid}")]
    [HasPermission(PermissionCodes.SchedulingView)]
    [ProducesResponseType(typeof(DoctorScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorScheduleDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var schedule = await _schedules.GetByIdAsync(id, cancellationToken);
        return schedule is null ? NotFound() : Ok(schedule);
    }

    [HttpGet("doctors/{doctorId:guid}/schedules")]
    [HasPermission(PermissionCodes.SchedulingView)]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorScheduleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DoctorScheduleDto>>> ListByDoctor(
        Guid doctorId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        return Ok(await _schedules.ListByDoctorAsync(doctorId, isActive, cancellationToken));
    }

    [HttpPut("schedules/{id:guid}")]
    [HasPermission(PermissionCodes.SchedulingUpdate)]
    [ProducesResponseType(typeof(DoctorScheduleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DoctorScheduleDto>> Update(
        Guid id,
        [FromBody] UpdateDoctorScheduleRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _schedules.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("schedules/{id:guid}/activate")]
    [HasPermission(PermissionCodes.SchedulingActivate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _schedules.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("schedules/{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.SchedulingDeactivate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _schedules.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("doctors/{doctorId:guid}/availability")]
    [HasPermission(PermissionCodes.SchedulingAvailabilityView)]
    [ProducesResponseType(typeof(DoctorAvailabilityDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DoctorAvailabilityDto>> GetAvailability(
        Guid doctorId,
        [FromQuery] DateOnly date,
        [FromQuery] Guid? clinicId,
        CancellationToken cancellationToken)
    {
        return Ok(await _availability.GetAvailabilityAsync(doctorId, date, clinicId, cancellationToken));
    }
}
