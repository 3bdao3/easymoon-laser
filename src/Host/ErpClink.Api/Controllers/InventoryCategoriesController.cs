using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Inventory.Application.Categories;
using ErpClink.Modules.Inventory.Application.Categories.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/inventory/categories")]
public sealed class InventoryCategoriesController : ControllerBase
{
    private readonly IInventoryCategoryService _categories;

    public InventoryCategoriesController(IInventoryCategoryService categories) => _categories = categories;

    [HttpPost]
    [HasPermission(PermissionCodes.InventoryCategoriesCreate)]
    public async Task<ActionResult<InventoryCategoryDto>> Create([FromBody] CreateInventoryCategoryRequest request, CancellationToken cancellationToken)
    {
        var dto = await _categories.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.InventoryCategoriesView)]
    public async Task<ActionResult<InventoryCategoryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _categories.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet]
    [HasPermission(PermissionCodes.InventoryCategoriesView)]
    public async Task<ActionResult<PagedInventoryCategoriesResult>> Search([FromQuery] SearchInventoryCategoriesRequest request, CancellationToken cancellationToken) =>
        Ok(await _categories.SearchAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.InventoryCategoriesUpdate)]
    public async Task<ActionResult<InventoryCategoryDto>> Update(Guid id, [FromBody] UpdateInventoryCategoryRequest request, CancellationToken cancellationToken) =>
        Ok(await _categories.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [HasPermission(PermissionCodes.InventoryCategoriesActivate)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _categories.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.InventoryCategoriesDeactivate)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _categories.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
