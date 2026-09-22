using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Assets.Application.Accounting.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/assets")]
public sealed class AssetAccountingController : ControllerBase
{
    private readonly IAssetAccountingService _accounting;

    public AssetAccountingController(IAssetAccountingService accounting) => _accounting = accounting;

    [HttpGet("accounting")]
    [HasPermission(PermissionCodes.AssetsAccountingView)]
    public async Task<ActionResult<PagedAssetAccountingResult>> Search(
        [FromQuery] SearchAssetAccountingRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _accounting.SearchAsync(request, cancellationToken));

    [HttpGet("accounting/{assetId:guid}")]
    [HasPermission(PermissionCodes.AssetsAccountingView)]
    public async Task<ActionResult<AssetFinancialProfileDto>> GetProfile(Guid assetId, CancellationToken cancellationToken)
    {
        var dto = await _accounting.GetProfileAsync(assetId, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("accounting/{assetId:guid}/history")]
    [HasPermission(PermissionCodes.AssetsHistoryView)]
    public async Task<ActionResult<PagedAssetFinancialHistoryResult>> History(
        Guid assetId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await _accounting.GetFinancialHistoryAsync(assetId, page, pageSize, cancellationToken));

    [HttpPost("accounting/capitalize")]
    [HasPermission(PermissionCodes.AssetsAccountingCapitalize)]
    public async Task<ActionResult<AssetFinancialProfileDto>> Capitalize(
        [FromBody] CapitalizeAssetRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _accounting.CapitalizeAsync(request, cancellationToken));

    [HttpGet("depreciation")]
    [HasPermission(PermissionCodes.AssetsDepreciationView)]
    public async Task<ActionResult<PagedDepreciationTransactionsResult>> Depreciation(
        [FromQuery] SearchDepreciationTransactionsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _accounting.SearchDepreciationAsync(request, cancellationToken));

    [HttpGet("depreciation/schedule")]
    [HasPermission(PermissionCodes.AssetsDepreciationSchedule)]
    public async Task<ActionResult<PagedDepreciationScheduleResult>> Schedule(
        [FromQuery] Guid assetId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await _accounting.GetScheduleAsync(assetId, page, pageSize, cancellationToken));

    [HttpPost("depreciation/post")]
    [HasPermission(PermissionCodes.AssetsDepreciationPost)]
    public async Task<ActionResult<DepreciationTransactionDto>> Post(
        [FromBody] PostDepreciationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _accounting.PostDepreciationAsync(request, cancellationToken));

    [HttpGet("valuation")]
    [HasPermission(PermissionCodes.AssetsAccountingView)]
    public async Task<ActionResult<AssetValuationResult>> Valuation(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await _accounting.GetValuationAsync(page, pageSize, cancellationToken));

    [HttpGet("disposals")]
    [HasPermission(PermissionCodes.AssetsDisposalView)]
    public async Task<ActionResult<PagedAssetDisposalsResult>> Disposals(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await _accounting.SearchDisposalsAsync(page, pageSize, cancellationToken));

    [HttpPost("disposals")]
    [HasPermission(PermissionCodes.AssetsDisposalProcess)]
    public async Task<ActionResult<AssetFinancialProfileDto>> DisposeFinancial(
        [FromBody] DisposeAssetFinancialRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _accounting.DisposeFinancialAsync(request, cancellationToken));
}
