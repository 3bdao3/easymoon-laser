using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Inventory.Application.GoodsReceipts;
using ErpClink.Modules.Inventory.Application.GoodsReceipts.Models;
using ErpClink.Modules.Procurement.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/inventory/goods-receipts")]
public sealed class InventoryGoodsReceiptsController : ControllerBase
{
    private readonly IGoodsReceiptService _goodsReceipts;
    private readonly IPurchaseOrderReceivingPort _poReceiving;

    public InventoryGoodsReceiptsController(IGoodsReceiptService goodsReceipts, IPurchaseOrderReceivingPort poReceiving)
    {
        _goodsReceipts = goodsReceipts;
        _poReceiving = poReceiving;
    }

    [HttpGet("receivable-purchase-orders/{purchaseOrderId:guid}")]
    [HasPermission(PermissionCodes.InventoryGoodsReceiptsCreate)]
    public async Task<ActionResult<PurchaseOrderReceivingSnapshot>> GetReceivablePurchaseOrder(
        Guid purchaseOrderId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _poReceiving.GetReceivableAsync(purchaseOrderId, cancellationToken);
        return snapshot is null ? NotFound() : Ok(snapshot);
    }

    [HttpPost("from-purchase-order")]
    [HasPermission(PermissionCodes.InventoryGoodsReceiptsCreate)]
    public async Task<ActionResult<GoodsReceiptDto>> CreateFromPurchaseOrder(
        [FromBody] CreateGoodsReceiptFromPoRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await _goodsReceipts.CreateFromPurchaseOrderAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.InventoryGoodsReceiptsView)]
    public async Task<ActionResult<GoodsReceiptDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _goodsReceipts.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet]
    [HasPermission(PermissionCodes.InventoryGoodsReceiptsView)]
    public async Task<ActionResult<PagedGoodsReceiptsResult>> Search([FromQuery] SearchGoodsReceiptsRequest request, CancellationToken cancellationToken) =>
        Ok(await _goodsReceipts.SearchAsync(request, cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(PermissionCodes.InventoryGoodsReceiptsCancel)]
    public async Task<ActionResult<GoodsReceiptDto>> Cancel(Guid id, [FromBody] CancelGoodsReceiptRequest request, CancellationToken cancellationToken) =>
        Ok(await _goodsReceipts.CancelAsync(id, request, cancellationToken));
}
