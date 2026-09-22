using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Appointments.Application.Appointments;
using ErpClink.Modules.Appointments.Application.Appointments.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/appointments")]
public sealed class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointments;

    public AppointmentsController(IAppointmentService appointments)
    {
        _appointments = appointments;
    }

    [HttpPost]
    [HasPermission(PermissionCodes.AppointmentsCreate)]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AppointmentDto>> Book(
        [FromBody] BookAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var appointment = await _appointments.BookAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.AppointmentsView)]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByIdAsync(id, cancellationToken);
        return appointment is null ? NotFound() : Ok(appointment);
    }

    [HttpGet("by-number/{appointmentNumber}")]
    [HasPermission(PermissionCodes.AppointmentsView)]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> GetByNumber(string appointmentNumber, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByNumberAsync(appointmentNumber, cancellationToken);
        return appointment is null ? NotFound() : Ok(appointment);
    }

    [HttpGet("search")]
    [HasPermission(PermissionCodes.AppointmentsView)]
    [ProducesResponseType(typeof(PagedAppointmentsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedAppointmentsResult>> Search(
        [FromQuery] SearchAppointmentsRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _appointments.SearchAsync(request, cancellationToken));
    }

    [HttpPost("{id:guid}/confirm")]
    [HasPermission(PermissionCodes.AppointmentsConfirm)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        await _appointments.ConfirmAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(PermissionCodes.AppointmentsCancel)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelAppointmentRequest? request,
        CancellationToken cancellationToken)
    {
        await _appointments.CancelAsync(id, request ?? new CancelAppointmentRequest(null), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reschedule")]
    [HasPermission(PermissionCodes.AppointmentsReschedule)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reschedule(
        Guid id,
        [FromBody] RescheduleAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        await _appointments.RescheduleAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/no-show")]
    [HasPermission(PermissionCodes.AppointmentsNoShow)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkNoShow(Guid id, CancellationToken cancellationToken)
    {
        await _appointments.MarkNoShowAsync(id, cancellationToken);
        return NoContent();
    }
}
