using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.LaserClinic.Application.Appointments;
using ErpClink.Modules.LaserClinic.Domain.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/laser-appointments")]
public sealed class LaserAppointmentsController : ControllerBase
{
    private readonly ILaserAppointmentAppService _appointments;
    private readonly ICurrentUser _currentUser;

    public LaserAppointmentsController(ILaserAppointmentAppService appointments, ICurrentUser currentUser)
    {
        _appointments = appointments;
        _currentUser = currentUser;
    }

    [HttpGet]
    [HasPermission(PermissionCodes.LaserAppointmentsView)]
    public async Task<ActionResult<IReadOnlyList<LaserAppointmentDto>>> List(
        [FromQuery] DateOnly? date,
        [FromQuery] Guid? customerId,
        CancellationToken cancellationToken)
        => Ok(await _appointments.ListAsync(date, customerId, cancellationToken));

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.LaserAppointmentsView)]
    public async Task<ActionResult<LaserAppointmentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _appointments.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("availability")]
    [HasPermission(PermissionCodes.LaserAppointmentsView)]
    public async Task<ActionResult<AvailabilityResultDto>> Availability(
        [FromQuery] DateOnly date,
        [FromQuery] Guid[] serviceIds,
        [FromQuery] Guid? excludeAppointmentId,
        [FromQuery] string[]? durationOverrides,
        CancellationToken cancellationToken)
        => Ok(await _appointments.GetAvailabilityAsync(
            new AvailabilityQuery(
                date,
                serviceIds ?? [],
                excludeAppointmentId,
                ParseDurationOverrides(durationOverrides)),
            cancellationToken));

    [HttpGet("check-slot")]
    [HasPermission(PermissionCodes.LaserAppointmentsView)]
    public async Task<ActionResult<SlotCheckResultDto>> CheckSlot(
        [FromQuery] DateOnly date,
        [FromQuery] TimeOnly startTime,
        [FromQuery] Guid[] serviceIds,
        [FromQuery] string[]? durationOverrides,
        CancellationToken cancellationToken)
        => Ok(await _appointments.CheckSlotAsync(
            new SlotCheckQuery(date, startTime, serviceIds ?? [], ParseDurationOverrides(durationOverrides)),
            cancellationToken));

    [HttpPost]
    [HasPermission(PermissionCodes.LaserAppointmentsCreate)]
    public async Task<ActionResult<LaserAppointmentDto>> Create(
        [FromBody] CreateLaserAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _appointments.CreateAsync(request, _currentUser.UserId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("book-with-customer")]
    [HasPermission(PermissionCodes.LaserAppointmentsCreate)]
    public async Task<ActionResult<LaserAppointmentDto>> BookWithCustomer(
        [FromBody] CreateBookingWithCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _appointments.CreateBookingWithCustomerAsync(request, _currentUser.UserId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.LaserAppointmentsUpdate)]
    public async Task<ActionResult<LaserAppointmentDto>> Update(
        Guid id,
        [FromBody] UpdateLaserAppointmentRequest request,
        CancellationToken cancellationToken)
        => Ok(await _appointments.UpdateAsync(id, request, _currentUser.UserId, cancellationToken));

    [HttpGet("{id:guid}/session")]
    [HasPermission(PermissionCodes.LaserAppointmentsView)]
    public async Task<ActionResult<SessionDetailDto>> GetSession(Guid id, CancellationToken cancellationToken)
    {
        var item = await _appointments.GetSessionDetailAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("{id:guid}/session")]
    [HasPermission(PermissionCodes.LaserAppointmentsUpdate)]
    public async Task<ActionResult<SessionDetailDto>> RecordSession(
        Guid id,
        [FromBody] RecordSessionRequest request,
        CancellationToken cancellationToken)
        => Ok(await _appointments.RecordSessionAsync(id, request, _currentUser.UserId, cancellationToken));

    [HttpPut("{id:guid}/status")]
    [HasPermission(PermissionCodes.LaserAppointmentsUpdate)]
    public async Task<ActionResult<LaserAppointmentDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateLaserAppointmentStatusRequest request,
        CancellationToken cancellationToken)
        => Ok(await _appointments.UpdateStatusAsync(id, request.Status, _currentUser.UserId, cancellationToken));

    [HttpDelete("{id:guid}")]
    [HasPermission(PermissionCodes.LaserAppointmentsCancel)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _appointments.DeleteAsync(id, _currentUser.UserId, cancellationToken);
        return NoContent();
    }

    private static IReadOnlyDictionary<Guid, int>? ParseDurationOverrides(string[]? raw)
    {
        if (raw is null || raw.Length == 0)
            return null;

        var map = new Dictionary<Guid, int>();
        foreach (var entry in raw)
        {
            if (string.IsNullOrWhiteSpace(entry))
                continue;
            var parts = entry.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2
                || !Guid.TryParse(parts[0], out var serviceId)
                || !int.TryParse(parts[1], out var minutes))
                continue;
            map[serviceId] = minutes;
        }

        return map.Count == 0 ? null : map;
    }
}

[ApiController]
[Authorize]
[Route("api/v1/customers/{customerId:guid}/history")]
public sealed class CustomerHistoryController : ControllerBase
{
    private readonly ILaserAppointmentAppService _appointments;

    public CustomerHistoryController(ILaserAppointmentAppService appointments) => _appointments = appointments;

    [HttpGet]
    [HasPermission(PermissionCodes.LaserCustomersView)]
    public async Task<ActionResult<CustomerHistoryDto>> Get(Guid customerId, CancellationToken cancellationToken)
        => Ok(await _appointments.GetCustomerHistoryAsync(customerId, cancellationToken));
}
