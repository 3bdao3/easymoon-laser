using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Services.Application.Catalog;
using ErpClink.Modules.Services.Application.Catalog.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/services")]
public sealed class ServicesController : ControllerBase
{
    private readonly IHealthcareServiceCatalogService _services;

    public ServicesController(IHealthcareServiceCatalogService services) => _services = services;

    [HttpPost]
    [HasPermission(PermissionCodes.ServicesCreate)]
    [ProducesResponseType(typeof(HealthcareServiceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<HealthcareServiceDto>> Create(
        [FromBody] CreateHealthcareServiceRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await _services.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.ServicesView)]
    public async Task<ActionResult<HealthcareServiceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _services.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("search")]
    [HasPermission(PermissionCodes.ServicesView)]
    public async Task<ActionResult<PagedHealthcareServicesResult>> Search(
        [FromQuery] SearchHealthcareServicesRequest request,
        CancellationToken cancellationToken)
        => Ok(await _services.SearchAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.ServicesUpdate)]
    public async Task<ActionResult<HealthcareServiceDto>> Update(
        Guid id,
        [FromBody] UpdateHealthcareServiceRequest request,
        CancellationToken cancellationToken)
        => Ok(await _services.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [HasPermission(PermissionCodes.ServicesActivate)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _services.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.ServicesDeactivate)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _services.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
