using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ErpClink.BuildingBlocks.Application.Common;
using ErpClink.Modules.Administration.Application.Auth.Models;
using ErpClink.Modules.Administration.Application.Users.Models;
using ErpClink.Modules.Administration.Domain.Auth;
using ErpClink.Modules.Administration.Domain.Users;
using ErpClink.Modules.Administration.Infrastructure.Auth;
using ErpClink.Modules.Administration.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Api.Tests;

public sealed class AuthApiTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthApiTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Valid_login_succeeds()
    {
        var client = _factory.CreateClient();
        var response = await LoginAsAdminAsync(client);
        response.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.RefreshToken.Should().NotBeNullOrWhiteSpace();
        response.User.Email.Should().Be("admin@erpclink.local");
        response.User.Roles.Should().Contain(AppRoles.SuperAdmin);
        response.User.Permissions.Should().Contain(PermissionCodes.AdministrationUsersView);
    }

    [Fact]
    public async Task Invalid_password_fails()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@erpclink.local", "WrongPassword!1"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Inactive_user_cannot_login()
    {
        var client = _factory.CreateClient();
        var admin = await LoginAsAdminAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var createResponse = await client.PostAsJsonAsync("/api/admin/users", new CreateUserRequest(
            $"inactive-{Guid.NewGuid():N}@erpclink.local",
            "TempUser!123",
            "Inactive User",
            null,
            [AppRoles.Receptionist]));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<UserDto>(JsonOptions);
        created.Should().NotBeNull();

        var deactivate = await client.PostAsync($"/api/admin/users/{created!.Id}/deactivate", null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        client.DefaultRequestHeaders.Authorization = null;
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(created.Email, "TempUser!123"));
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_token_works()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsAdminAsync(client);
        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(login.RefreshToken));
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await refresh.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBe(login.RefreshToken);
    }

    [Fact]
    public async Task Expired_refresh_token_fails()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsAdminAsync(client);
        var hash = TokenHashing.Hash(login.RefreshToken);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AdministrationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [admin].[RefreshTokens] SET [ExpiresAtUtc] = {DateTime.UtcNow.AddMinutes(-5)} WHERE [TokenHash] = {hash}");
        }

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(login.RefreshToken));
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoked_refresh_token_fails()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsAdminAsync(client);
        var logout = await client.PostAsJsonAsync("/api/auth/logout", new LogoutRequest(login.RefreshToken));
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(login.RefreshToken));
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Permission_authorization_succeeds_when_permission_exists()
    {
        var client = _factory.CreateClient();
        var login = await LoginAsAdminAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.GetAsync("/api/v1/secure/patients-view");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);
    }

    [Fact]
    public async Task Permission_authorization_fails_when_permission_missing()
    {
        var client = _factory.CreateClient();
        var admin = await LoginAsAdminAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);

        var email = $"recv-{Guid.NewGuid():N}@erpclink.local";
        var createResponse = await client.PostAsJsonAsync("/api/admin/users", new CreateUserRequest(
            email,
            "TempUser!123",
            "Reception User",
            null,
            [AppRoles.Receptionist]));
        createResponse.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = null;
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "TempUser!123"));
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        var denied = await client.GetAsync("/api/admin/users");
        denied.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Seed_is_idempotent()
    {
        await AdministrationDbSeeder.SeedAsync(_factory.Services);
        await AdministrationDbSeeder.SeedAsync(_factory.Services);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AdministrationDbContext>();
        var roleCount = await db.Roles.CountAsync();
        var permissionCount = await db.Permissions.CountAsync();
        var adminCount = await db.Users.CountAsync(u => u.Email == "admin@erpclink.local");
        var receptionistCount = await db.Users.CountAsync(u => u.UserName == "socialmedia");

        roleCount.Should().Be(AppRoles.All.Count);
        permissionCount.Should().BeGreaterThan(10);
        adminCount.Should().Be(1);
        receptionistCount.Should().Be(1);
    }

    private static async Task<AuthResponse> LoginAsAdminAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@erpclink.local", "ChangeMe!Admin123"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        body.Should().NotBeNull();
        return body!;
    }
}
