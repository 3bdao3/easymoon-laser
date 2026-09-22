using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.AspNetCore.Auth;
using ErpClink.BuildingBlocks.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.BuildingBlocks.AspNetCore;

public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksAspNetCore(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        return services;
    }
}
