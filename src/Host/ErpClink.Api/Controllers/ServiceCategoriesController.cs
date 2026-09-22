using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Services.Application.Categories;
using ErpClink.Modules.Services.Application.Categories.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/service-categories")]
public sealed class ServiceCategoriesController : ControllerBase
{
    private readonly IServiceCategoryService _categories;

    public ServiceCategoriesController(IServiceCategoryService categories) => _categories = categories;

    [HttpPost]
    [HasPermission(PermissionCodes.ServicesCreate)]
    [ProducesResponseType(typeof(ServiceCategoryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ServiceCategoryDto>> Create(
        [FromBody] CreateServiceCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await _categories.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.ServicesView)]
    public async Task<ActionResult<ServiceCategoryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _categories.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("search")]
    [HasPermission(PermissionCodes.ServicesView)]
    public async Task<ActionResult<PagedServiceCategoriesResult>> Search(
        [FromQuery] SearchServiceCategoriesRequest request,
        CancellationToken cancellationToken)
        => Ok(await _categories.SearchAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.ServicesUpdate)]
    public async Task<ActionResult<ServiceCategoryDto>> Update(
        Guid id,
        [FromBody] UpdateServiceCategoryRequest request,
        CancellationToken cancellationToken)
        => Ok(await _categories.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [HasPermission(PermissionCodes.ServicesActivate)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _categories.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.ServicesDeactivate)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _categories.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
