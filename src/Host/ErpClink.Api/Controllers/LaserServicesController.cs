using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.LaserClinic.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/laser-services")]
public sealed class LaserServicesController : ControllerBase
{
    private readonly ILaserServiceCatalogAppService _services;
    private readonly ICurrentUser _currentUser;

    public LaserServicesController(ILaserServiceCatalogAppService services, ICurrentUser currentUser)
    {
        _services = services;
        _currentUser = currentUser;
    }

    [HttpGet]
    [HasPermission(PermissionCodes.LaserServicesView)]
    public async Task<ActionResult<IReadOnlyList<LaserServiceDto>>> List(
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
        => Ok(await _services.ListAsync(activeOnly, cancellationToken));

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.LaserServicesView)]
    public async Task<ActionResult<LaserServiceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _services.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermission(PermissionCodes.LaserServicesManage)]
    public async Task<ActionResult<LaserServiceDto>> Create(
        [FromBody] CreateLaserServiceRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _services.CreateAsync(request, _currentUser.UserId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.LaserServicesManage)]
    public async Task<ActionResult<LaserServiceDto>> Update(
        Guid id,
        [FromBody] UpdateLaserServiceRequest request,
        CancellationToken cancellationToken)
        => Ok(await _services.UpdateAsync(id, request, _currentUser.UserId, cancellationToken));
}
