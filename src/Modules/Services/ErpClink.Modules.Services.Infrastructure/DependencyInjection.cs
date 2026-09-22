using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Options;
using ErpClink.BuildingBlocks.Infrastructure.Organization;
using ErpClink.BuildingBlocks.Infrastructure.Time;
using ErpClink.Modules.Services.Application.Catalog;
using ErpClink.Modules.Services.Application.Categories;
using ErpClink.Modules.Services.Application.Common;
using ErpClink.Modules.Services.Application.Contracts;
using ErpClink.Modules.Services.Application.Packages;
using ErpClink.Modules.Services.Application.Validators;
using ErpClink.Modules.Services.Infrastructure.Catalog;
using ErpClink.Modules.Services.Infrastructure.Categories;
using ErpClink.Modules.Services.Infrastructure.Contracts;
using ErpClink.Modules.Services.Infrastructure.Events;
using ErpClink.Modules.Services.Infrastructure.Packages;
using ErpClink.Modules.Services.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ErpClink.Modules.Services.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddServicesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrganizationOptions>(configuration.GetSection(OrganizationOptions.SectionName));
        services.TryAddSingleton<IOrganizationContext, ConfigurationOrganizationContext>();
        services.TryAddSingleton<IBusinessClock, UtcBusinessClock>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ServicesDbContext>(options => options.UseSqlServer(connectionString));

        services.AddValidatorsFromAssemblyContaining<CreateHealthcareServiceRequestValidator>();

        services.AddScoped<IHealthcareServiceCatalogService, HealthcareServiceCatalogService>();
        services.AddScoped<IServiceCategoryService, ServiceCategoryService>();
        services.AddScoped<IHealthcarePackageService, HealthcarePackageService>();
        services.AddScoped<IServiceLookup, ServiceLookup>();
        services.AddScoped<IPackageLookup, PackageLookup>();

        services.AddSingleton<ServicesDomainEventCollector>();
        services.AddScoped<IServicesDomainEventDispatcher, LoggingServicesDomainEventDispatcher>();

        return services;
    }

    public static async Task MigrateServicesModuleAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServicesDbContext>();
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync(cancellationToken);
        else
            await db.Database.EnsureCreatedAsync(cancellationToken);
    }
}
