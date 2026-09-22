using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Administration.Application.Users;
using ErpClink.Modules.Administration.Application.Users.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpClink.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    [HasPermission(PermissionCodes.AdministrationUsersView)]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _userManagementService.ListAsync(cancellationToken));
    }

    [HttpGet("{id}")]
    [HasPermission(PermissionCodes.AdministrationUsersView)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var user = await _userManagementService.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [HasPermission(PermissionCodes.AdministrationUsersCreate)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManagementService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    [HasPermission(PermissionCodes.AdministrationUsersUpdate)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Update(string id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _userManagementService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id}/activate")]
    [HasPermission(PermissionCodes.AdministrationUsersUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Activate(string id, CancellationToken cancellationToken)
    {
        await _userManagementService.SetActiveAsync(id, true, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/deactivate")]
    [HasPermission(PermissionCodes.AdministrationUsersUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(string id, CancellationToken cancellationToken)
    {
        await _userManagementService.SetActiveAsync(id, false, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/roles")]
    [HasPermission(PermissionCodes.AdministrationRolesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignRole(string id, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        await _userManagementService.AssignRoleAsync(id, request.RoleName, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}/roles/{roleName}")]
    [HasPermission(PermissionCodes.AdministrationRolesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveRole(string id, string roleName, CancellationToken cancellationToken)
    {
        await _userManagementService.RemoveRoleAsync(id, roleName, cancellationToken);
        return NoContent();
    }
}
