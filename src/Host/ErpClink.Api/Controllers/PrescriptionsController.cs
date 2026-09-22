using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Prescriptions.Application.Prescriptions;
using ErpClink.Modules.Prescriptions.Application.Prescriptions.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/prescriptions")]
public sealed class PrescriptionsController : ControllerBase
{
    private readonly IPrescriptionService _prescriptions;

    public PrescriptionsController(IPrescriptionService prescriptions) => _prescriptions = prescriptions;

    [HttpPost]
    [HasPermission(PermissionCodes.PrescriptionsCreate)]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PrescriptionDto>> Create(
        [FromBody] CreatePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        var rx = await _prescriptions.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = rx.Id }, rx);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.PrescriptionsView)]
    public async Task<ActionResult<PrescriptionDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var rx = await _prescriptions.GetByIdAsync(id, cancellationToken);
        return rx is null ? NotFound() : Ok(rx);
    }

    [HttpGet("by-number/{prescriptionNumber}")]
    [HasPermission(PermissionCodes.PrescriptionsView)]
    public async Task<ActionResult<PrescriptionDto>> GetByNumber(string prescriptionNumber, CancellationToken cancellationToken)
    {
        var rx = await _prescriptions.GetByNumberAsync(prescriptionNumber, cancellationToken);
        return rx is null ? NotFound() : Ok(rx);
    }

    [HttpGet("search")]
    [HasPermission(PermissionCodes.PrescriptionsView)]
    public async Task<ActionResult<PagedPrescriptionsResult>> Search(
        [FromQuery] SearchPrescriptionsRequest request,
        CancellationToken cancellationToken)
        => Ok(await _prescriptions.SearchAsync(request, cancellationToken));

    [HttpGet("patients/{patientId:guid}/history")]
    [HasPermission(PermissionCodes.PrescriptionsView)]
    public async Task<ActionResult<PagedPrescriptionsResult>> PatientHistory(
        Guid patientId,
        [FromQuery] PatientPrescriptionHistoryRequest request,
        CancellationToken cancellationToken)
        => Ok(await _prescriptions.GetPatientHistoryAsync(patientId, request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.PrescriptionsUpdate)]
    public async Task<ActionResult<PrescriptionDto>> Update(
        Guid id,
        [FromBody] UpdatePrescriptionRequest request,
        CancellationToken cancellationToken)
        => Ok(await _prescriptions.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/items")]
    [HasPermission(PermissionCodes.PrescriptionsUpdate)]
    public async Task<ActionResult<PrescriptionDto>> AddItem(
        Guid id,
        [FromBody] PrescriptionItemInput request,
        CancellationToken cancellationToken)
        => Ok(await _prescriptions.AddItemAsync(id, request, cancellationToken));

    [HttpPut("{id:guid}/items/{itemId:guid}")]
    [HasPermission(PermissionCodes.PrescriptionsUpdate)]
    public async Task<ActionResult<PrescriptionDto>> UpdateItem(
        Guid id,
        Guid itemId,
        [FromBody] UpdatePrescriptionItemRequest request,
        CancellationToken cancellationToken)
        => Ok(await _prescriptions.UpdateItemAsync(id, itemId, request, cancellationToken));

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [HasPermission(PermissionCodes.PrescriptionsUpdate)]
    public async Task<ActionResult<PrescriptionDto>> RemoveItem(
        Guid id,
        Guid itemId,
        [FromQuery] byte[]? rowVersion,
        CancellationToken cancellationToken)
        => Ok(await _prescriptions.RemoveItemAsync(id, itemId, rowVersion, cancellationToken));

    [HttpPost("{id:guid}/issue")]
    [HasPermission(PermissionCodes.PrescriptionsIssue)]
    public async Task<IActionResult> Issue(Guid id, CancellationToken cancellationToken)
    {
        await _prescriptions.IssueAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(PermissionCodes.PrescriptionsCancel)]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelPrescriptionRequest? request,
        CancellationToken cancellationToken)
    {
        await _prescriptions.CancelAsync(id, request ?? new CancelPrescriptionRequest(null), cancellationToken);
        return NoContent();
    }
}
