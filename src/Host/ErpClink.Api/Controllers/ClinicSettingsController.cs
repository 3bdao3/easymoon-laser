using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.LaserClinic.Application.Dashboard;
using ErpClink.Modules.LaserClinic.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/clinic-settings")]
public sealed class ClinicSettingsController : ControllerBase
{
    private readonly IClinicSettingsAppService _settings;
    private readonly ICurrentUser _currentUser;

    public ClinicSettingsController(IClinicSettingsAppService settings, ICurrentUser currentUser)
    {
        _settings = settings;
        _currentUser = currentUser;
    }

    [HttpGet]
    [HasPermission(PermissionCodes.LaserSettingsView)]
    public async Task<ActionResult<ClinicSettingsDto>> Get(CancellationToken cancellationToken)
        => Ok(await _settings.GetAsync(cancellationToken));

    [HttpPut]
    [HasPermission(PermissionCodes.LaserSettingsManage)]
    public async Task<ActionResult<ClinicSettingsDto>> Update(
        [FromBody] UpdateClinicSettingsRequest request,
        CancellationToken cancellationToken)
        => Ok(await _settings.UpdateAsync(request, _currentUser.UserId, cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/v1/laser-dashboard")]
public sealed class LaserDashboardController : ControllerBase
{
    private readonly ILaserDashboardAppService _dashboard;

    public LaserDashboardController(ILaserDashboardAppService dashboard) => _dashboard = dashboard;

    [HttpGet]
    [HasPermission(PermissionCodes.LaserDashboardView)]
    public async Task<ActionResult<LaserDashboardDto>> Get(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
        => Ok(await _dashboard.GetAsync(date, cancellationToken));
}
