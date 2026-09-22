using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Billing.Application.Payments;
using ErpClink.Modules.Billing.Application.Payments.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/billing/payments")]
public sealed class BillingPaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;

    public BillingPaymentsController(IPaymentService payments) => _payments = payments;

    [HttpPost]
    [HasPermission(PermissionCodes.FinancePaymentsCreate)]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PaymentDto>> Record(
        [FromBody] RecordPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var payment = await _payments.RecordAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.FinancePaymentsView)]
    public async Task<ActionResult<PaymentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var payment = await _payments.GetByIdAsync(id, cancellationToken);
        return payment is null ? NotFound() : Ok(payment);
    }

    [HttpGet("search")]
    [HasPermission(PermissionCodes.FinancePaymentsView)]
    public async Task<ActionResult<PagedPaymentsResult>> Search(
        [FromQuery] SearchPaymentsRequest request,
        CancellationToken cancellationToken)
        => Ok(await _payments.SearchAsync(request, cancellationToken));

    [HttpPost("{id:guid}/reverse")]
    [HasPermission(PermissionCodes.FinancePaymentsReverse)]
    public async Task<ActionResult<PaymentDto>> Reverse(
        Guid id,
        [FromBody] ReversePaymentRequest request,
        CancellationToken cancellationToken)
        => Ok(await _payments.ReverseAsync(id, request, cancellationToken));
}
