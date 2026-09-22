using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.LaserClinic.Application.Offers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/laser-offers")]
public sealed class LaserOffersController : ControllerBase
{
    private readonly ILaserOfferAppService _offers;
    private readonly ICurrentUser _currentUser;

    public LaserOffersController(ILaserOfferAppService offers, ICurrentUser currentUser)
    {
        _offers = offers;
        _currentUser = currentUser;
    }

    [HttpGet]
    [HasPermission(PermissionCodes.LaserOffersView)]
    public async Task<ActionResult<IReadOnlyList<LaserOfferDto>>> List(
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
        => Ok(await _offers.ListAsync(activeOnly, cancellationToken));

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.LaserOffersView)]
    public async Task<ActionResult<LaserOfferDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _offers.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermission(PermissionCodes.LaserOffersManage)]
    public async Task<ActionResult<LaserOfferDto>> Create(
        [FromBody] CreateLaserOfferRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _offers.CreateAsync(request, _currentUser.UserId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.LaserOffersManage)]
    public async Task<ActionResult<LaserOfferDto>> Update(
        Guid id,
        [FromBody] UpdateLaserOfferRequest request,
        CancellationToken cancellationToken)
        => Ok(await _offers.UpdateAsync(id, request, _currentUser.UserId, cancellationToken));
}
