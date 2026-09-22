using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Assets.Application.Categories;
using ErpClink.Modules.Assets.Application.Categories.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/assets/categories")]
public sealed class AssetsCategoriesController : ControllerBase
{
    private readonly IAssetCategoryService _categories;

    public AssetsCategoriesController(IAssetCategoryService categories) => _categories = categories;

    [HttpPost]
    [HasPermission(PermissionCodes.AssetsCategoriesCreate)]
    public async Task<ActionResult<AssetCategoryDto>> Create([FromBody] CreateAssetCategoryRequest request, CancellationToken cancellationToken)
    {
        var dto = await _categories.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.AssetsCategoriesView)]
    public async Task<ActionResult<AssetCategoryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _categories.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet]
    [HasPermission(PermissionCodes.AssetsCategoriesView)]
    public async Task<ActionResult<PagedAssetCategoriesResult>> Search([FromQuery] SearchAssetCategoriesRequest request, CancellationToken cancellationToken) =>
        Ok(await _categories.SearchAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.AssetsCategoriesUpdate)]
    public async Task<ActionResult<AssetCategoryDto>> Update(Guid id, [FromBody] UpdateAssetCategoryRequest request, CancellationToken cancellationToken) =>
        Ok(await _categories.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [HasPermission(PermissionCodes.AssetsCategoriesActivate)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _categories.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.AssetsCategoriesDeactivate)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _categories.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
