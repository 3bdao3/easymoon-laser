using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Finance.Application.TrialBalance;
using ErpClink.Modules.Finance.Application.TrialBalance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/finance/trial-balance")]
public sealed class FinanceTrialBalanceController : ControllerBase
{
    private readonly ITrialBalanceService _trialBalance;

    public FinanceTrialBalanceController(ITrialBalanceService trialBalance) => _trialBalance = trialBalance;

    [HttpGet]
    [HasPermission(PermissionCodes.FinanceTrialBalanceView)]
    public async Task<ActionResult<TrialBalanceResult>> Get([FromQuery] GetTrialBalanceRequest request, CancellationToken cancellationToken) =>
        Ok(await _trialBalance.GetAsync(request, cancellationToken));
}
