using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Inventory.Application.Costing.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/inventory/valuation")]
public sealed class InventoryValuationController : ControllerBase
{
    private readonly IInventoryValuationQueryService _valuation;

    public InventoryValuationController(IInventoryValuationQueryService valuation) => _valuation = valuation;

    [HttpGet]
    [HasPermission(PermissionCodes.InventoryValuationView)]
    public async Task<ActionResult<ValuationResult>> Get([FromQuery] ValuationQueryRequest request, CancellationToken cancellationToken) =>
        Ok(await _valuation.GetValuationAsync(request, cancellationToken));

    [HttpGet("cost-history")]
    [HasPermission(PermissionCodes.InventoryCostHistoryView)]
    public async Task<ActionResult<PagedCostHistoryResult>> CostHistory(
        [FromQuery] CostHistoryQueryRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _valuation.GetCostHistoryAsync(request, cancellationToken));

    [HttpGet("cost-layers")]
    [HasPermission(PermissionCodes.InventoryCostLayersView)]
    public async Task<ActionResult<PagedCostLayersResult>> CostLayers(
        [FromQuery] CostLayerQueryRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _valuation.GetCostLayersAsync(request, cancellationToken));

    [HttpGet("issue-costs")]
    [HasPermission(PermissionCodes.InventoryIssueCostView)]
    public async Task<ActionResult<PagedIssueCostResult>> IssueCosts(
        [FromQuery] IssueCostQueryRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _valuation.GetIssueCostsAsync(request, cancellationToken));
}
