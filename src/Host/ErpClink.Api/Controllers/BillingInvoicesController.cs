using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Billing.Application.Invoices;
using ErpClink.Modules.Billing.Application.Invoices.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/billing/invoices")]
public sealed class BillingInvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoices;

    public BillingInvoicesController(IInvoiceService invoices) => _invoices = invoices;

    [HttpPost]
    [HasPermission(PermissionCodes.FinanceInvoicesCreate)]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<InvoiceDto>> CreateDraft(
        [FromBody] CreateDraftInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoices.CreateDraftAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.FinanceInvoicesView)]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _invoices.GetByIdAsync(id, cancellationToken);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpGet("by-number/{invoiceNumber}")]
    [HasPermission(PermissionCodes.FinanceInvoicesView)]
    public async Task<ActionResult<InvoiceDto>> GetByNumber(string invoiceNumber, CancellationToken cancellationToken)
    {
        var invoice = await _invoices.GetByNumberAsync(invoiceNumber, cancellationToken);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpGet("search")]
    [HasPermission(PermissionCodes.FinanceInvoicesView)]
    public async Task<ActionResult<PagedInvoicesResult>> Search(
        [FromQuery] SearchInvoicesRequest request,
        CancellationToken cancellationToken)
        => Ok(await _invoices.SearchAsync(request, cancellationToken));

    [HttpGet("patients/{patientId:guid}")]
    [HasPermission(PermissionCodes.FinanceInvoicesView)]
    public async Task<ActionResult<PagedInvoicesResult>> PatientInvoices(
        Guid patientId,
        [FromQuery] PatientInvoicesRequest request,
        CancellationToken cancellationToken)
        => Ok(await _invoices.GetPatientInvoicesAsync(patientId, request, cancellationToken));

    [HttpGet("patients/{patientId:guid}/outstanding")]
    [HasPermission(PermissionCodes.FinanceInvoicesView)]
    public async Task<ActionResult<PatientOutstandingDto>> PatientOutstanding(
        Guid patientId,
        CancellationToken cancellationToken)
        => Ok(await _invoices.GetPatientOutstandingAsync(patientId, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.FinanceInvoicesUpdate)]
    public async Task<ActionResult<InvoiceDto>> UpdateDraft(
        Guid id,
        [FromBody] UpdateDraftInvoiceRequest request,
        CancellationToken cancellationToken)
        => Ok(await _invoices.UpdateDraftAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/lines")]
    [HasPermission(PermissionCodes.FinanceInvoicesUpdate)]
    public async Task<ActionResult<InvoiceDto>> AddLine(
        Guid id,
        [FromBody] AddInvoiceLineRequest request,
        CancellationToken cancellationToken)
        => Ok(await _invoices.AddLineAsync(id, request, cancellationToken));

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    [HasPermission(PermissionCodes.FinanceInvoicesUpdate)]
    public async Task<ActionResult<InvoiceDto>> UpdateLine(
        Guid id,
        Guid lineId,
        [FromBody] UpdateInvoiceLineRequest request,
        CancellationToken cancellationToken)
        => Ok(await _invoices.UpdateLineAsync(id, lineId, request, cancellationToken));

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    [HasPermission(PermissionCodes.FinanceInvoicesUpdate)]
    public async Task<ActionResult<InvoiceDto>> RemoveLine(
        Guid id,
        Guid lineId,
        [FromQuery] byte[]? rowVersion,
        CancellationToken cancellationToken)
        => Ok(await _invoices.RemoveLineAsync(id, lineId, rowVersion, cancellationToken));

    [HttpPost("{id:guid}/issue")]
    [HasPermission(PermissionCodes.FinanceInvoicesIssue)]
    public async Task<ActionResult<InvoiceDto>> Issue(
        Guid id,
        [FromQuery] byte[]? rowVersion,
        CancellationToken cancellationToken)
        => Ok(await _invoices.IssueAsync(id, rowVersion, cancellationToken));

    [HttpPost("{id:guid}/void")]
    [HasPermission(PermissionCodes.FinanceInvoicesVoid)]
    public async Task<ActionResult<InvoiceDto>> Void(
        Guid id,
        [FromBody] VoidInvoiceRequest request,
        CancellationToken cancellationToken)
        => Ok(await _invoices.VoidAsync(id, request, cancellationToken));
}
