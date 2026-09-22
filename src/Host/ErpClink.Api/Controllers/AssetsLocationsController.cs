using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Assets.Application.Locations;
using ErpClink.Modules.Assets.Application.Locations.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/assets/locations")]
public sealed class AssetsLocationsController : ControllerBase
{
    private readonly IAssetLocationService _locations;

    public AssetsLocationsController(IAssetLocationService locations) => _locations = locations;

    [HttpPost]
    [HasPermission(PermissionCodes.AssetsLocationsCreate)]
    public async Task<ActionResult<AssetLocationDto>> Create([FromBody] CreateAssetLocationRequest request, CancellationToken cancellationToken)
    {
        var dto = await _locations.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.AssetsLocationsView)]
    public async Task<ActionResult<AssetLocationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _locations.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet]
    [HasPermission(PermissionCodes.AssetsLocationsView)]
    public async Task<ActionResult<PagedAssetLocationsResult>> Search([FromQuery] SearchAssetLocationsRequest request, CancellationToken cancellationToken) =>
        Ok(await _locations.SearchAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.AssetsLocationsUpdate)]
    public async Task<ActionResult<AssetLocationDto>> Update(Guid id, [FromBody] UpdateAssetLocationRequest request, CancellationToken cancellationToken) =>
        Ok(await _locations.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [HasPermission(PermissionCodes.AssetsLocationsActivate)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _locations.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.AssetsLocationsDeactivate)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _locations.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
