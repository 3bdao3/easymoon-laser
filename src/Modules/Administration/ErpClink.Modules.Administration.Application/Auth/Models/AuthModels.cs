namespace ErpClink.Modules.Administration.Application.Auth.Models;

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record AuthUserDto(
    string Id,
    string Email,
    string? UserName,
    string FullName,
    bool IsActive,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    AuthUserDto User);
