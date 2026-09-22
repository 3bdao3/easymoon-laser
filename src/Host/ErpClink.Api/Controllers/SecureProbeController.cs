using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

/// <summary>
/// Minimal protected endpoint used to verify JWT + permission authorization.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/secure")]
public sealed class SecureProbeController : ControllerBase
{
    private readonly ICurrentUser _currentUser;

    public SecureProbeController(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Me()
    {
        return Ok(new
        {
            _currentUser.UserId,
            _currentUser.UserName,
            _currentUser.Email,
            _currentUser.Roles,
            _currentUser.Permissions,
            _currentUser.IsAuthenticated
        });
    }

    [HttpGet("patients-view")]
    [HasPermission(ErpClink.BuildingBlocks.Application.Common.PermissionCodes.PatientsView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult PatientsViewProbe()
    {
        return Ok(new { message = "Permission Patients.View granted." });
    }
}
