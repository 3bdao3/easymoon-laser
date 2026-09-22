using Microsoft.AspNetCore.Authorization;

namespace ErpClink.BuildingBlocks.AspNetCore.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim(c =>
                c.Type == "permission" &&
                string.Equals(c.Value, requirement.PermissionCode, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
