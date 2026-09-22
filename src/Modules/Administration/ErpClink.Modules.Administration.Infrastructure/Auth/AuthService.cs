using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.Modules.Administration.Application.Auth;
using ErpClink.Modules.Administration.Application.Auth.Models;
using ErpClink.Modules.Administration.Application.Common;
using ErpClink.Modules.Administration.Application.Options;
using ErpClink.Modules.Administration.Domain.Auth;
using ErpClink.Modules.Administration.Infrastructure.Identity;
using ErpClink.Modules.Administration.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ErpClink.Modules.Administration.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AdministrationDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _clock;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        AdministrationDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider clock,
        IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _clock = clock;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new AppException("auth.invalid_request", "Email/username and password are required.", 400);
        }

        var identifier = request.Email.Trim();
        var user = await _userManager.FindByEmailAsync(identifier)
                   ?? await _userManager.FindByNameAsync(identifier);
        if (user is null)
        {
            throw new AppException("auth.invalid_credentials", "Invalid email or password.", 401);
        }

        if (!user.IsActive)
        {
            throw new AppException("auth.user_inactive", "User account is inactive.", 401);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new AppException("auth.locked_out", "User account is locked.", 401);
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            throw new AppException("auth.invalid_credentials", "Invalid email or password.", 401);
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        user.LastLoginAtUtc = _clock.UtcNow;
        await _userManager.UpdateAsync(user);

        return await IssueTokensAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new AppException("auth.invalid_request", "Refresh token is required.", 400);
        }

        var hash = TokenHashing.Hash(request.RefreshToken);
        var existing = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (existing is null)
        {
            throw new AppException("auth.invalid_refresh_token", "Invalid refresh token.", 401);
        }

        if (existing.IsRevoked)
        {
            // Reuse detection: revoke all active tokens for the user.
            await RevokeAllActiveTokensForUserAsync(existing.UserId, ipAddress, cancellationToken);
            throw new AppException("auth.refresh_token_revoked", "Refresh token has been revoked.", 401);
        }

        if (existing.IsExpired(_clock.UtcNow))
        {
            throw new AppException("auth.refresh_token_expired", "Refresh token has expired.", 401);
        }

        var user = await _userManager.FindByIdAsync(existing.UserId);
        if (user is null || !user.IsActive)
        {
            existing.Revoke(_clock.UtcNow, ipAddress);
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new AppException("auth.invalid_refresh_token", "Invalid refresh token.", 401);
        }

        var rawRefresh = TokenHashing.GenerateSecureToken();
        var replacement = RefreshToken.Create(
            user.Id,
            TokenHashing.Hash(rawRefresh),
            _clock.UtcNow,
            _clock.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            ipAddress);

        existing.Revoke(_clock.UtcNow, ipAddress, replacement.Id);
        _dbContext.RefreshTokens.Add(replacement);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetPermissionsForRolesAsync(roles, cancellationToken);
        var (accessToken, expiresAt) = _jwtTokenService.CreateAccessToken(user, roles, permissions);

        return new AuthResponse(
            accessToken,
            rawRefresh,
            expiresAt,
            new AuthUserDto(
                user.Id,
                user.Email ?? string.Empty,
                user.UserName,
                user.FullName,
                user.IsActive,
                roles.ToList(),
                permissions));
    }

    public async Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new AppException("auth.invalid_request", "Refresh token is required.", 400);
        }

        var hash = TokenHashing.Hash(request.RefreshToken);
        var existing = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (existing is null || existing.IsRevoked)
        {
            return;
        }

        existing.Revoke(_clock.UtcNow, ipAddress);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, string? ipAddress, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetPermissionsForRolesAsync(roles, cancellationToken);
        var (accessToken, expiresAt) = _jwtTokenService.CreateAccessToken(user, roles, permissions);

        var rawRefresh = TokenHashing.GenerateSecureToken();
        var refreshEntity = RefreshToken.Create(
            user.Id,
            TokenHashing.Hash(rawRefresh),
            _clock.UtcNow,
            _clock.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            ipAddress);

        _dbContext.RefreshTokens.Add(refreshEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            rawRefresh,
            expiresAt,
            new AuthUserDto(
                user.Id,
                user.Email ?? string.Empty,
                user.UserName,
                user.FullName,
                user.IsActive,
                roles.ToList(),
                permissions));
    }

    private async Task<IReadOnlyList<string>> GetPermissionsForRolesAsync(
        IList<string> roles,
        CancellationToken cancellationToken)
    {
        if (roles.Count == 0)
        {
            return Array.Empty<string>();
        }

        var roleIds = await _dbContext.Roles
            .Where(r => roles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
        {
            return Array.Empty<string>();
        }

        return await _dbContext.RolePermissions
            .AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Join(
                _dbContext.Permissions.AsNoTracking().Where(p => p.IsActive),
                rp => rp.PermissionId,
                p => p.Id,
                (_, p) => p.Code)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    private async Task RevokeAllActiveTokensForUserAsync(string userId, string? ipAddress, CancellationToken cancellationToken)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(_clock.UtcNow, ipAddress);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
