using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Queue.Application.Entries;
using ErpClink.Modules.Queue.Application.Entries.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/queue")]
public sealed class QueueController : ControllerBase
{
    private readonly IQueueService _queue;

    public QueueController(IQueueService queue) => _queue = queue;

    [HttpPost("check-in")]
    [HasPermission(PermissionCodes.QueueCheckIn)]
    [ProducesResponseType(typeof(QueueEntryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<QueueEntryDto>> CheckIn(
        [FromBody] CheckInRequest request,
        CancellationToken cancellationToken)
    {
        var entry = await _queue.CheckInAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.QueueView)]
    [ProducesResponseType(typeof(QueueEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QueueEntryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _queue.GetByIdAsync(id, cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpGet("today")]
    [HasPermission(PermissionCodes.QueueView)]
    [ProducesResponseType(typeof(IReadOnlyList<QueueListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<QueueListItemDto>>> Today(
        [FromQuery] TodayQueueRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _queue.GetTodayAsync(request, cancellationToken));
    }

    [HttpGet("search")]
    [HasPermission(PermissionCodes.QueueView)]
    [ProducesResponseType(typeof(PagedQueueResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedQueueResult>> Search(
        [FromQuery] SearchQueueRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _queue.SearchAsync(request, cancellationToken));
    }

    [HttpPost("{id:guid}/call")]
    [HasPermission(PermissionCodes.QueueCall)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Call(Guid id, CancellationToken cancellationToken)
    {
        await _queue.CallAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/start-service")]
    [HasPermission(PermissionCodes.QueueStartService)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> StartService(Guid id, CancellationToken cancellationToken)
    {
        await _queue.StartServiceAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    [HasPermission(PermissionCodes.QueueComplete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        await _queue.CompleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/skip")]
    [HasPermission(PermissionCodes.QueueSkip)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Skip(Guid id, CancellationToken cancellationToken)
    {
        await _queue.SkipAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(PermissionCodes.QueueCancel)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _queue.CancelAsync(id, cancellationToken);
        return NoContent();
    }
}
