using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Services.Application.Packages;
using ErpClink.Modules.Services.Application.Packages.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/packages")]
public sealed class PackagesController : ControllerBase
{
    private readonly IHealthcarePackageService _packages;

    public PackagesController(IHealthcarePackageService packages) => _packages = packages;

    [HttpPost]
    [HasPermission(PermissionCodes.PackagesCreate)]
    [ProducesResponseType(typeof(HealthcarePackageDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<HealthcarePackageDto>> Create(
        [FromBody] CreateHealthcarePackageRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await _packages.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.PackagesView)]
    public async Task<ActionResult<HealthcarePackageDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _packages.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("search")]
    [HasPermission(PermissionCodes.PackagesView)]
    public async Task<ActionResult<PagedHealthcarePackagesResult>> Search(
        [FromQuery] SearchHealthcarePackagesRequest request,
        CancellationToken cancellationToken)
        => Ok(await _packages.SearchAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.PackagesUpdate)]
    public async Task<ActionResult<HealthcarePackageDto>> Update(
        Guid id,
        [FromBody] UpdateHealthcarePackageRequest request,
        CancellationToken cancellationToken)
        => Ok(await _packages.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [HasPermission(PermissionCodes.PackagesActivate)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _packages.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.PackagesDeactivate)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _packages.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/items")]
    [HasPermission(PermissionCodes.PackagesUpdate)]
    public async Task<ActionResult<HealthcarePackageDto>> AddItem(
        Guid id,
        [FromBody] PackageItemInput request,
        CancellationToken cancellationToken)
        => Ok(await _packages.AddItemAsync(id, request, cancellationToken));

    [HttpPut("{id:guid}/items/{itemId:guid}")]
    [HasPermission(PermissionCodes.PackagesUpdate)]
    public async Task<ActionResult<HealthcarePackageDto>> UpdateItem(
        Guid id,
        Guid itemId,
        [FromBody] UpdatePackageItemRequest request,
        CancellationToken cancellationToken)
        => Ok(await _packages.UpdateItemAsync(id, itemId, request, cancellationToken));

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [HasPermission(PermissionCodes.PackagesUpdate)]
    public async Task<ActionResult<HealthcarePackageDto>> RemoveItem(
        Guid id,
        Guid itemId,
        [FromQuery] byte[]? rowVersion,
        CancellationToken cancellationToken)
        => Ok(await _packages.RemoveItemAsync(id, itemId, rowVersion, cancellationToken));
}
