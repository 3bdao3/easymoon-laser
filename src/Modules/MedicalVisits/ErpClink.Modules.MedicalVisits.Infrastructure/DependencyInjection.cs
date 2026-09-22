using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Options;
using ErpClink.BuildingBlocks.Infrastructure.Organization;
using ErpClink.BuildingBlocks.Infrastructure.Time;
using ErpClink.Modules.MedicalVisits.Application.Common;
using ErpClink.Modules.MedicalVisits.Application.Validators;
using ErpClink.Modules.MedicalVisits.Application.Visits;
using ErpClink.Modules.MedicalVisits.Infrastructure.Events;
using ErpClink.Modules.MedicalVisits.Infrastructure.Persistence;
using ErpClink.Modules.MedicalVisits.Infrastructure.Visits;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ErpClink.Modules.MedicalVisits.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMedicalVisitsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrganizationOptions>(configuration.GetSection(OrganizationOptions.SectionName));
        services.TryAddSingleton<IOrganizationContext, ConfigurationOrganizationContext>();
        services.TryAddSingleton<IBusinessClock, UtcBusinessClock>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<MedicalVisitsDbContext>(options => options.UseSqlServer(connectionString));

        services.AddValidatorsFromAssemblyContaining<StartMedicalVisitRequestValidator>();
        services.AddScoped<IVisitNumberGenerator, SequentialVisitNumberGenerator>();
        services.AddScoped<IMedicalVisitService, MedicalVisitService>();
        services.AddScoped<Application.Contracts.IMedicalVisitPrescriptionPort, Contracts.MedicalVisitPrescriptionPort>();
        services.AddScoped<Application.Contracts.IMedicalVisitBillingPort, Contracts.MedicalVisitBillingPort>();
        services.AddSingleton<MedicalVisitsDomainEventCollector>();
        services.AddScoped<IMedicalVisitsDomainEventDispatcher, LoggingMedicalVisitsDomainEventDispatcher>();

        return services;
    }

    public static async Task MigrateMedicalVisitsModuleAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MedicalVisitsDbContext>();
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync(cancellationToken);
        else
            await db.Database.EnsureCreatedAsync(cancellationToken);
    }
}
