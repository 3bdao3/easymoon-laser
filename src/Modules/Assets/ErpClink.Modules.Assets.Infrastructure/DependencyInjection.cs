using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Options;
using ErpClink.BuildingBlocks.Infrastructure.Organization;
using ErpClink.BuildingBlocks.Infrastructure.Time;
using ErpClink.Modules.Assets.Application.Accounting;
using ErpClink.Modules.Assets.Application.Accounting.Models;
using ErpClink.Modules.Assets.Application.Assets;
using ErpClink.Modules.Assets.Application.Categories;
using ErpClink.Modules.Assets.Application.Common;
using ErpClink.Modules.Assets.Application.Contracts;
using ErpClink.Modules.Assets.Application.Locations;
using ErpClink.Modules.Assets.Application.Validators;
using ErpClink.Modules.Assets.Infrastructure.Accounting;
using ErpClink.Modules.Assets.Infrastructure.Assets;
using ErpClink.Modules.Assets.Infrastructure.Categories;
using ErpClink.Modules.Assets.Infrastructure.Contracts;
using ErpClink.Modules.Assets.Infrastructure.Events;
using ErpClink.Modules.Assets.Infrastructure.Locations;
using ErpClink.Modules.Assets.Infrastructure.Numbering;
using ErpClink.Modules.Assets.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ErpClink.Modules.Assets.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAssetsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrganizationOptions>(configuration.GetSection(OrganizationOptions.SectionName));
        services.TryAddSingleton<IOrganizationContext, ConfigurationOrganizationContext>();
        services.TryAddSingleton<IBusinessClock, UtcBusinessClock>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AssetsDbContext>(options => options.UseSqlServer(connectionString));

        services.AddValidatorsFromAssemblyContaining<CreateAssetCategoryRequestValidator>();

        services.AddScoped<IAssetCategoryCodeGenerator, SequentialAssetCategoryCodeGenerator>();
        services.AddScoped<IAssetLocationCodeGenerator, SequentialAssetLocationCodeGenerator>();
        services.AddScoped<IAssetNumberGenerator, SequentialAssetNumberGenerator>();

        services.AddScoped<IAssetCategoryService, AssetCategoryService>();
        services.AddScoped<IAssetLocationService, AssetLocationService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IDepreciationMethod, StraightLineDepreciationMethod>();
        services.AddScoped<IAssetAccountingService, AssetAccountingService>();

        services.AddScoped<IAssetLookup, AssetLookup>();

        services.AddSingleton<AssetsDomainEventCollector>();
        services.AddScoped<IAssetsDomainEventDispatcher, LoggingAssetsDomainEventDispatcher>();

        return services;
    }

    public static async Task MigrateAssetsModuleAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AssetsDbContext>();
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync(cancellationToken);
        else
            await db.Database.EnsureCreatedAsync(cancellationToken);
    }
}
