using ErpClink.Modules.Administration.Application.Auth.Models;

namespace ErpClink.Modules.Administration.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken = default);
}
