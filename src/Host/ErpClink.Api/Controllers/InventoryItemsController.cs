using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Inventory.Application.Items;
using ErpClink.Modules.Inventory.Application.Items.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/inventory/items")]
public sealed class InventoryItemsController : ControllerBase
{
    private readonly IInventoryItemService _items;

    public InventoryItemsController(IInventoryItemService items) => _items = items;

    [HttpPost]
    [HasPermission(PermissionCodes.InventoryItemsCreate)]
    public async Task<ActionResult<InventoryItemDto>> Create([FromBody] CreateInventoryItemRequest request, CancellationToken cancellationToken)
    {
        var dto = await _items.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.InventoryItemsView)]
    public async Task<ActionResult<InventoryItemDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _items.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet]
    [HasPermission(PermissionCodes.InventoryItemsView)]
    public async Task<ActionResult<PagedInventoryItemsResult>> Search([FromQuery] SearchInventoryItemsRequest request, CancellationToken cancellationToken) =>
        Ok(await _items.SearchAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.InventoryItemsUpdate)]
    public async Task<ActionResult<InventoryItemDto>> Update(Guid id, [FromBody] UpdateInventoryItemRequest request, CancellationToken cancellationToken) =>
        Ok(await _items.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [HasPermission(PermissionCodes.InventoryItemsActivate)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _items.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.InventoryItemsDeactivate)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _items.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
