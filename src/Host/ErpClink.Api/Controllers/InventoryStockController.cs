using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Inventory.Application.Stock;
using ErpClink.Modules.Inventory.Application.Stock.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/inventory/stock")]
public sealed class InventoryStockController : ControllerBase
{
    private readonly IStockService _stock;

    public InventoryStockController(IStockService stock) => _stock = stock;

    [HttpGet("balances")]
    [HasPermission(PermissionCodes.InventoryStockView)]
    public async Task<ActionResult<PagedStockBalancesResult>> SearchBalances([FromQuery] SearchStockBalancesRequest request, CancellationToken cancellationToken) =>
        Ok(await _stock.SearchBalancesAsync(request, cancellationToken));

    [HttpPost("adjust")]
    [HasPermission(PermissionCodes.InventoryStockAdjust)]
    public async Task<IActionResult> Adjust([FromBody] AdjustStockRequest request, CancellationToken cancellationToken)
    {
        await _stock.AdjustAsync(request, cancellationToken);
        return NoContent();
    }
}
