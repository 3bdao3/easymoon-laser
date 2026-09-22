using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Finance.Application.FiscalYears;
using ErpClink.Modules.Finance.Application.FiscalYears.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/finance/fiscal-years")]
public sealed class FinanceFiscalYearsController : ControllerBase
{
    private readonly IFiscalYearService _years;

    public FinanceFiscalYearsController(IFiscalYearService years) => _years = years;

    [HttpPost]
    [HasPermission(PermissionCodes.FinanceFiscalYearsCreate)]
    public async Task<ActionResult<FiscalYearDto>> Create([FromBody] CreateFiscalYearRequest request, CancellationToken cancellationToken)
    {
        var dto = await _years.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.FinanceFiscalYearsView)]
    public async Task<ActionResult<FiscalYearDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _years.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet]
    [HasPermission(PermissionCodes.FinanceFiscalYearsView)]
    public async Task<ActionResult<PagedFiscalYearsResult>> Search([FromQuery] SearchFiscalYearsRequest request, CancellationToken cancellationToken) =>
        Ok(await _years.SearchAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.FinanceFiscalYearsUpdate)]
    public async Task<ActionResult<FiscalYearDto>> Update(Guid id, [FromBody] UpdateFiscalYearRequest request, CancellationToken cancellationToken) =>
        Ok(await _years.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/close")]
    [HasPermission(PermissionCodes.FinanceFiscalYearsClose)]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        await _years.CloseAsync(id, cancellationToken);
        return NoContent();
    }
}
