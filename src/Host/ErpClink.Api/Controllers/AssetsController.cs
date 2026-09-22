using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Assets.Application.Assets;
using ErpClink.Modules.Assets.Application.Assets.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/assets")]
public sealed class AssetsController : ControllerBase
{
    private readonly IAssetService _assets;

    public AssetsController(IAssetService assets) => _assets = assets;

    [HttpPost]
    [HasPermission(PermissionCodes.AssetsCreate)]
    public async Task<ActionResult<AssetDto>> Create([FromBody] CreateAssetRequest request, CancellationToken cancellationToken)
    {
        var dto = await _assets.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.AssetsView)]
    public async Task<ActionResult<AssetDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _assets.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet]
    [HasPermission(PermissionCodes.AssetsView)]
    public async Task<ActionResult<PagedAssetsResult>> Search([FromQuery] SearchAssetsRequest request, CancellationToken cancellationToken) =>
        Ok(await _assets.SearchAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.AssetsUpdate)]
    public async Task<ActionResult<AssetDto>> Update(Guid id, [FromBody] UpdateAssetRequest request, CancellationToken cancellationToken) =>
        Ok(await _assets.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/change-location")]
    [HasPermission(PermissionCodes.AssetsAssign)]
    public async Task<ActionResult<AssetDto>> ChangeLocation(Guid id, [FromBody] ChangeAssetLocationRequest request, CancellationToken cancellationToken) =>
        Ok(await _assets.ChangeLocationAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/start-maintenance")]
    [HasPermission(PermissionCodes.AssetsMaintenance)]
    public async Task<ActionResult<AssetDto>> StartMaintenance(Guid id, [FromBody] StartAssetMaintenanceRequest request, CancellationToken cancellationToken) =>
        Ok(await _assets.StartMaintenanceAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/complete-maintenance")]
    [HasPermission(PermissionCodes.AssetsMaintenance)]
    public async Task<ActionResult<AssetDto>> CompleteMaintenance(Guid id, [FromBody] CompleteAssetMaintenanceRequest request, CancellationToken cancellationToken) =>
        Ok(await _assets.CompleteMaintenanceAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/retire")]
    [HasPermission(PermissionCodes.AssetsRetire)]
    public async Task<ActionResult<AssetDto>> Retire(Guid id, [FromBody] RetireAssetRequest request, CancellationToken cancellationToken) =>
        Ok(await _assets.RetireAsync(id, request, cancellationToken));

    [HttpGet("{id:guid}/history")]
    [HasPermission(PermissionCodes.AssetsHistoryView)]
    public async Task<ActionResult<IReadOnlyList<AssetHistoryEntryDto>>> History(Guid id, CancellationToken cancellationToken) =>
        Ok(await _assets.GetHistoryAsync(id, cancellationToken));
}
