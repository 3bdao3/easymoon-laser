using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Finance.Application.FiscalPeriods;
using ErpClink.Modules.Finance.Application.FiscalPeriods.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/finance/fiscal-periods")]
public sealed class FinanceFiscalPeriodsController : ControllerBase
{
    private readonly IFiscalPeriodService _periods;

    public FinanceFiscalPeriodsController(IFiscalPeriodService periods) => _periods = periods;

    [HttpPost]
    [HasPermission(PermissionCodes.FinanceFiscalPeriodsCreate)]
    public async Task<ActionResult<FiscalPeriodDto>> Create([FromBody] CreateFiscalPeriodRequest request, CancellationToken cancellationToken)
    {
        var dto = await _periods.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.FinanceFiscalPeriodsView)]
    public async Task<ActionResult<FiscalPeriodDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _periods.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet]
    [HasPermission(PermissionCodes.FinanceFiscalPeriodsView)]
    public async Task<ActionResult<PagedFiscalPeriodsResult>> Search([FromQuery] SearchFiscalPeriodsRequest request, CancellationToken cancellationToken) =>
        Ok(await _periods.SearchAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.FinanceFiscalPeriodsUpdate)]
    public async Task<ActionResult<FiscalPeriodDto>> Update(Guid id, [FromBody] UpdateFiscalPeriodRequest request, CancellationToken cancellationToken) =>
        Ok(await _periods.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/close")]
    [HasPermission(PermissionCodes.FinanceFiscalPeriodsClose)]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        await _periods.CloseAsync(id, cancellationToken);
        return NoContent();
    }
}
