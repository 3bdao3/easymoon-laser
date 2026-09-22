using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Procurement.Application.PurchaseOrders;
using ErpClink.Modules.Procurement.Application.PurchaseOrders.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/procurement/purchase-orders")]
public sealed class ProcurementPurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrders;

    public ProcurementPurchaseOrdersController(IPurchaseOrderService purchaseOrders) => _purchaseOrders = purchaseOrders;

    [HttpPost]
    [HasPermission(PermissionCodes.ProcurementPurchaseOrdersCreate)]
    [ProducesResponseType(typeof(PurchaseOrderDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PurchaseOrderDto>> CreateDraft(
        [FromBody] CreateDraftPurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        var po = await _purchaseOrders.CreateDraftAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = po.Id }, po);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.ProcurementPurchaseOrdersView)]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var po = await _purchaseOrders.GetByIdAsync(id, cancellationToken);
        return po is null ? NotFound() : Ok(po);
    }

    [HttpGet("by-number/{purchaseOrderNumber}")]
    [HasPermission(PermissionCodes.ProcurementPurchaseOrdersView)]
    public async Task<ActionResult<PurchaseOrderDto>> GetByNumber(string purchaseOrderNumber, CancellationToken cancellationToken)
    {
        var po = await _purchaseOrders.GetByNumberAsync(purchaseOrderNumber, cancellationToken);
        return po is null ? NotFound() : Ok(po);
    }

    [HttpGet("search")]
    [HasPermission(PermissionCodes.ProcurementPurchaseOrdersView)]
    public async Task<ActionResult<PagedPurchaseOrdersResult>> Search(
        [FromQuery] SearchPurchaseOrdersRequest request,
        CancellationToken cancellationToken)
        => Ok(await _purchaseOrders.SearchAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.ProcurementPurchaseOrdersUpdate)]
    public async Task<ActionResult<PurchaseOrderDto>> UpdateDraft(
        Guid id,
        [FromBody] UpdateDraftPurchaseOrderRequest request,
        CancellationToken cancellationToken)
        => Ok(await _purchaseOrders.UpdateDraftAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/lines")]
    [HasPermission(PermissionCodes.ProcurementPurchaseOrdersUpdate)]
    public async Task<ActionResult<PurchaseOrderDto>> AddLine(
        Guid id,
        [FromBody] AddPurchaseOrderLineRequest request,
        CancellationToken cancellationToken)
        => Ok(await _purchaseOrders.AddLineAsync(id, request, cancellationToken));

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    [HasPermission(PermissionCodes.ProcurementPurchaseOrdersUpdate)]
    public async Task<ActionResult<PurchaseOrderDto>> UpdateLine(
        Guid id,
        Guid lineId,
        [FromBody] UpdatePurchaseOrderLineRequest request,
        CancellationToken cancellationToken)
        => Ok(await _purchaseOrders.UpdateLineAsync(id, lineId, request, cancellationToken));

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    [HasPermission(PermissionCodes.ProcurementPurchaseOrdersUpdate)]
    public async Task<ActionResult<PurchaseOrderDto>> RemoveLine(
        Guid id,
        Guid lineId,
        [FromQuery] byte[]? rowVersion,
        CancellationToken cancellationToken)
        => Ok(await _purchaseOrders.RemoveLineAsync(id, lineId, rowVersion, cancellationToken));

    [HttpPost("{id:guid}/discount")]
    [HasPermission(PermissionCodes.ProcurementPurchaseOrdersUpdate)]
    public async Task<ActionResult<PurchaseOrderDto>> SetDiscount(
        Guid id,
        [FromBody] SetPurchaseOrderDiscountRequest request,
        CancellationToken cancellationToken)
        => Ok(await _purchaseOrders.SetDiscountAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/submit")]
    [HasPermission(PermissionCodes.ProcurementPurchaseOrdersSubmit)]
    public async Task<ActionResult<PurchaseOrderDto>> Submit(
        Guid id,
        [FromQuery] byte[]? rowVersion,
        CancellationToken cancellationToken)
        => Ok(await _purchaseOrders.SubmitAsync(id, rowVersion, cancellationToken));

    [HttpPost("{id:guid}/approve")]
    [HasPermission(PermissionCodes.ProcurementPurchaseOrdersApprove)]
    public async Task<ActionResult<PurchaseOrderDto>> Approve(
        Guid id,
        [FromQuery] byte[]? rowVersion,
        CancellationToken cancellationToken)
        => Ok(await _purchaseOrders.ApproveAsync(id, rowVersion, cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(PermissionCodes.ProcurementPurchaseOrdersCancel)]
    public async Task<ActionResult<PurchaseOrderDto>> Cancel(
        Guid id,
        [FromBody] CancelPurchaseOrderRequest request,
        CancellationToken cancellationToken)
        => Ok(await _purchaseOrders.CancelAsync(id, request, cancellationToken));
}
