using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Finance.Application.GeneralLedger;
using ErpClink.Modules.Finance.Application.GeneralLedger.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/finance/general-ledger")]
public sealed class FinanceGeneralLedgerController : ControllerBase
{
    private readonly IGeneralLedgerService _ledger;

    public FinanceGeneralLedgerController(IGeneralLedgerService ledger) => _ledger = ledger;

    [HttpGet]
    [HasPermission(PermissionCodes.FinanceGeneralLedgerView)]
    public async Task<ActionResult<PagedGeneralLedgerResult>> Search([FromQuery] SearchGeneralLedgerRequest request, CancellationToken cancellationToken) =>
        Ok(await _ledger.SearchAsync(request, cancellationToken));
}
