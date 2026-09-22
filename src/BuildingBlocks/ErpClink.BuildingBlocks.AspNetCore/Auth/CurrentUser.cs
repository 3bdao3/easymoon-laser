using ErpClink.BuildingBlocks.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ErpClink.BuildingBlocks.AspNetCore.Auth;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string? UserId =>
        Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Principal?.FindFirstValue("sub");

    public string? UserName =>
        Principal?.FindFirstValue(ClaimTypes.Name)
        ?? Principal?.Identity?.Name;

    public string? Email =>
        Principal?.FindFirstValue(ClaimTypes.Email)
        ?? Principal?.FindFirstValue("email");

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        ?? Array.Empty<string>();

    public IReadOnlyCollection<string> Permissions =>
        Principal?.FindAll("permission").Select(c => c.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        ?? Array.Empty<string>();

    public bool HasPermission(string permissionCode) =>
        Permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
}
