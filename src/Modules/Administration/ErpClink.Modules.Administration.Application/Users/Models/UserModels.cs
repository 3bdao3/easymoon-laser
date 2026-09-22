namespace ErpClink.Modules.Administration.Application.Users.Models;

public sealed record CreateUserRequest(
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber,
    IReadOnlyList<string>? Roles);

public sealed record UpdateUserRequest(
    string FullName,
    string? PhoneNumber,
    string? Email);

public sealed record SetUserActiveRequest(bool IsActive);

public sealed record AssignRoleRequest(string RoleName);

public sealed record UserDto(
    string Id,
    string Email,
    string? UserName,
    string FullName,
    string? PhoneNumber,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    IReadOnlyList<string> Roles);
