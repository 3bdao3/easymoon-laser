using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using ErpClink.Modules.Administration.Domain.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ErpClink.Modules.Administration.UnitTests;

public sealed class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task Succeeds_when_permission_claim_exists()
    {
        var handler = new PermissionAuthorizationHandler();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("permission", "Patients.View")
        ], "test"));

        var context = new AuthorizationHandlerContext(
            [new PermissionRequirement("Patients.View")],
            user,
            null);

        await handler.HandleAsync(context);
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Fails_when_permission_claim_missing()
    {
        var handler = new PermissionAuthorizationHandler();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("permission", "Patients.Create")
        ], "test"));

        var context = new AuthorizationHandlerContext(
            [new PermissionRequirement("Patients.View")],
            user,
            null);

        await handler.HandleAsync(context);
        context.HasSucceeded.Should().BeFalse();
    }
}

public sealed class RefreshTokenTests
{
    [Fact]
    public void Revoked_token_is_not_active()
    {
        var token = RefreshToken.Create("user-1", "hash", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), "127.0.0.1");
        token.Revoke(DateTime.UtcNow, "127.0.0.1");
        token.IsActive(DateTime.UtcNow).Should().BeFalse();
        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void Expired_token_is_not_active()
    {
        var token = RefreshToken.Create("user-1", "hash", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1), null);
        token.IsActive(DateTime.UtcNow).Should().BeFalse();
        token.IsExpired(DateTime.UtcNow).Should().BeTrue();
    }
}
