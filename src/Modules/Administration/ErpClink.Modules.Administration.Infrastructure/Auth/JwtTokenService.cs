using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ErpClink.Modules.Administration.Application.Options;
using ErpClink.Modules.Administration.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ErpClink.Modules.Administration.Infrastructure.Auth;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(
        ApplicationUser user,
        IEnumerable<string> roles,
        IEnumerable<string> permissions);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(
        ApplicationUser user,
        IEnumerable<string> roles,
        IEnumerable<string> permissions)
    {
        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Authentication:Jwt:SigningKey must be configured and at least 32 characters.");
        }

        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        // Keep claim types explicit when MapInboundClaims=false
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        claims.AddRange(roles.Distinct(StringComparer.OrdinalIgnoreCase).Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Distinct(StringComparer.OrdinalIgnoreCase).Select(p => new Claim("permission", p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
