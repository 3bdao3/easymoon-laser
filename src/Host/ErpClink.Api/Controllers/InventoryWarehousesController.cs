using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Inventory.Application.Warehouses;
using ErpClink.Modules.Inventory.Application.Warehouses.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/inventory/warehouses")]
public sealed class InventoryWarehousesController : ControllerBase
{
    private readonly IWarehouseService _warehouses;

    public InventoryWarehousesController(IWarehouseService warehouses) => _warehouses = warehouses;

    [HttpPost]
    [HasPermission(PermissionCodes.InventoryWarehousesCreate)]
    public async Task<ActionResult<WarehouseDto>> Create([FromBody] CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var dto = await _warehouses.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.InventoryWarehousesView)]
    public async Task<ActionResult<WarehouseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _warehouses.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet]
    [HasPermission(PermissionCodes.InventoryWarehousesView)]
    public async Task<ActionResult<PagedWarehousesResult>> Search([FromQuery] SearchWarehousesRequest request, CancellationToken cancellationToken) =>
        Ok(await _warehouses.SearchAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.InventoryWarehousesUpdate)]
    public async Task<ActionResult<WarehouseDto>> Update(Guid id, [FromBody] UpdateWarehouseRequest request, CancellationToken cancellationToken) =>
        Ok(await _warehouses.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [HasPermission(PermissionCodes.InventoryWarehousesActivate)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _warehouses.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.InventoryWarehousesDeactivate)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _warehouses.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
