using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Finance.Application.Subledgers;
using ErpClink.Modules.Finance.Application.Subledgers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/finance/ar")]
public sealed class FinanceArController : ControllerBase
{
    private readonly IArSubledgerQueryService _ar;

    public FinanceArController(IArSubledgerQueryService ar) => _ar = ar;

    [HttpGet("customers/{partyId:guid}/balance")]
    [HasPermission(PermissionCodes.FinanceArView)]
    public async Task<ActionResult<PartyBalanceDto>> GetBalance(Guid partyId, CancellationToken cancellationToken) =>
        Ok(await _ar.GetCustomerBalanceAsync(partyId, cancellationToken));

    [HttpGet("customers/{partyId:guid}/statement")]
    [HasPermission(PermissionCodes.FinanceArStatement)]
    public async Task<ActionResult<StatementResult>> GetStatement(
        Guid partyId,
        [FromQuery] StatementRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _ar.GetCustomerStatementAsync(request with { PartyId = partyId }, cancellationToken));

    [HttpGet("transactions")]
    [HasPermission(PermissionCodes.FinanceArView)]
    public async Task<ActionResult<PagedSubledgerTransactionsResult>> Search(
        [FromQuery] SearchSubledgerTransactionsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _ar.SearchAsync(request, cancellationToken));

    [HttpGet("aging")]
    [HasPermission(PermissionCodes.FinanceAgingView)]
    public async Task<ActionResult<AgingResult>> Aging([FromQuery] AgingRequest request, CancellationToken cancellationToken) =>
        Ok(await _ar.GetAgingAsync(request, cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/v1/finance/ap")]
public sealed class FinanceApController : ControllerBase
{
    private readonly IApSubledgerQueryService _ap;

    public FinanceApController(IApSubledgerQueryService ap) => _ap = ap;

    [HttpGet("suppliers/{partyId:guid}/balance")]
    [HasPermission(PermissionCodes.FinanceApView)]
    public async Task<ActionResult<PartyBalanceDto>> GetBalance(Guid partyId, CancellationToken cancellationToken) =>
        Ok(await _ap.GetSupplierBalanceAsync(partyId, cancellationToken));

    [HttpGet("suppliers/{partyId:guid}/statement")]
    [HasPermission(PermissionCodes.FinanceApStatement)]
    public async Task<ActionResult<StatementResult>> GetStatement(
        Guid partyId,
        [FromQuery] StatementRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _ap.GetSupplierStatementAsync(request with { PartyId = partyId }, cancellationToken));

    [HttpGet("transactions")]
    [HasPermission(PermissionCodes.FinanceApView)]
    public async Task<ActionResult<PagedSubledgerTransactionsResult>> Search(
        [FromQuery] SearchSubledgerTransactionsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _ap.SearchAsync(request, cancellationToken));

    [HttpGet("aging")]
    [HasPermission(PermissionCodes.FinanceAgingView)]
    public async Task<ActionResult<AgingResult>> Aging([FromQuery] AgingRequest request, CancellationToken cancellationToken) =>
        Ok(await _ap.GetAgingAsync(request, cancellationToken));
}
