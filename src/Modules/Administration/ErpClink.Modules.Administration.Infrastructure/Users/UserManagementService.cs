using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.Modules.Administration.Application.Common;
using ErpClink.Modules.Administration.Application.Users;
using ErpClink.Modules.Administration.Application.Users.Models;
using ErpClink.Modules.Administration.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Administration.Infrastructure.Users;

public sealed class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new AppException("users.invalid_request", "Email and password are required.", 400);
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new AppException("users.invalid_request", "Full name is required.", 400);
        }

        var existing = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (existing is not null)
        {
            throw new AppException("users.email_exists", "A user with this email already exists.", 400);
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true,
            PhoneNumber = request.PhoneNumber,
            FullName = request.FullName.Trim(),
            IsActive = true,
            CreatedAtUtc = _clock.UtcNow,
            CreatedBy = _currentUser.UserId
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        EnsureSuccess(result, "users.create_failed");

        if (request.Roles is { Count: > 0 })
        {
            foreach (var role in request.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                await EnsureRoleExistsAsync(role);
                var roleResult = await _userManager.AddToRoleAsync(user, role);
                EnsureSuccess(roleResult, "users.assign_role_failed");
            }
        }

        return await MapAsync(user);
    }

    public async Task<UserDto> UpdateAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredUserAsync(userId);

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new AppException("users.invalid_request", "Full name is required.", 400);
        }

        if (!string.IsNullOrWhiteSpace(request.Email) &&
            !string.Equals(user.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var other = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (other is not null && other.Id != user.Id)
            {
                throw new AppException("users.email_exists", "A user with this email already exists.", 400);
            }

            user.Email = request.Email.Trim();
            user.UserName = request.Email.Trim();
        }

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        user.UpdatedAtUtc = _clock.UtcNow;
        user.UpdatedBy = _currentUser.UserId;

        var result = await _userManager.UpdateAsync(user);
        EnsureSuccess(result, "users.update_failed");
        return await MapAsync(user);
    }

    public async Task SetActiveAsync(string userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredUserAsync(userId);
        user.IsActive = isActive;
        user.UpdatedAtUtc = _clock.UtcNow;
        user.UpdatedBy = _currentUser.UserId;
        var result = await _userManager.UpdateAsync(user);
        EnsureSuccess(result, "users.update_failed");
    }

    public async Task AssignRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new AppException("users.invalid_request", "Role name is required.", 400);
        }

        var user = await GetRequiredUserAsync(userId);
        await EnsureRoleExistsAsync(roleName.Trim());

        if (await _userManager.IsInRoleAsync(user, roleName.Trim()))
        {
            return;
        }

        var result = await _userManager.AddToRoleAsync(user, roleName.Trim());
        EnsureSuccess(result, "users.assign_role_failed");
    }

    public async Task RemoveRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new AppException("users.invalid_request", "Role name is required.", 400);
        }

        var user = await GetRequiredUserAsync(userId);
        if (!await _userManager.IsInRoleAsync(user, roleName.Trim()))
        {
            return;
        }

        var result = await _userManager.RemoveFromRoleAsync(user, roleName.Trim());
        EnsureSuccess(result, "users.remove_role_failed");
    }

    public async Task<UserDto?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is null ? null : await MapAsync(user);
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        var result = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            result.Add(await MapAsync(user));
        }

        return result;
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        throw new AppException("users.role_not_found", $"Role '{roleName}' was not found.", 400);
    }

    private async Task<ApplicationUser> GetRequiredUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            throw new AppException("users.not_found", "User was not found.", 404);
        }

        return user;
    }

    private async Task<UserDto> MapAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.UserName,
            user.FullName,
            user.PhoneNumber,
            user.IsActive,
            user.CreatedAtUtc,
            user.LastLoginAtUtc,
            roles.ToList());
    }

    private static void EnsureSuccess(IdentityResult result, string code)
    {
        if (result.Succeeded)
        {
            return;
        }

        var message = string.Join(" ", result.Errors.Select(e => e.Description));
        throw new AppException(code, message, 400);
    }
}
