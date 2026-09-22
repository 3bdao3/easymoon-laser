using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Finance.Application.Accounts;
using ErpClink.Modules.Finance.Application.Accounts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/finance/accounts")]
public sealed class FinanceAccountsController : ControllerBase
{
    private readonly IAccountService _accounts;

    public FinanceAccountsController(IAccountService accounts) => _accounts = accounts;

    [HttpPost]
    [HasPermission(PermissionCodes.FinanceAccountsCreate)]
    public async Task<ActionResult<AccountDto>> Create([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var dto = await _accounts.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.FinanceAccountsView)]
    public async Task<ActionResult<AccountDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _accounts.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet]
    [HasPermission(PermissionCodes.FinanceAccountsView)]
    public async Task<ActionResult<PagedAccountsResult>> Search([FromQuery] SearchAccountsRequest request, CancellationToken cancellationToken) =>
        Ok(await _accounts.SearchAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.FinanceAccountsUpdate)]
    public async Task<ActionResult<AccountDto>> Update(Guid id, [FromBody] UpdateAccountRequest request, CancellationToken cancellationToken) =>
        Ok(await _accounts.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [HasPermission(PermissionCodes.FinanceAccountsActivate)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _accounts.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.FinanceAccountsDeactivate)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _accounts.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
