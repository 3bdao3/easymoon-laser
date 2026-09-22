using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Options;
using ErpClink.BuildingBlocks.Infrastructure.Organization;
using ErpClink.BuildingBlocks.Infrastructure.Time;
using ErpClink.Modules.Prescriptions.Application.Common;
using ErpClink.Modules.Prescriptions.Application.Medications;
using ErpClink.Modules.Prescriptions.Application.Prescriptions;
using ErpClink.Modules.Prescriptions.Application.Validators;
using ErpClink.Modules.Prescriptions.Infrastructure.Events;
using ErpClink.Modules.Prescriptions.Infrastructure.Medications;
using ErpClink.Modules.Prescriptions.Infrastructure.Persistence;
using ErpClink.Modules.Prescriptions.Infrastructure.Prescriptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ErpClink.Modules.Prescriptions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPrescriptionsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrganizationOptions>(configuration.GetSection(OrganizationOptions.SectionName));
        services.TryAddSingleton<IOrganizationContext, ConfigurationOrganizationContext>();
        services.TryAddSingleton<IBusinessClock, UtcBusinessClock>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<PrescriptionsDbContext>(options => options.UseSqlServer(connectionString));

        services.AddValidatorsFromAssemblyContaining<CreatePrescriptionRequestValidator>();
        services.AddScoped<IPrescriptionNumberGenerator, SequentialPrescriptionNumberGenerator>();
        services.AddScoped<IMedicationService, MedicationService>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddSingleton<PrescriptionsDomainEventCollector>();
        services.AddScoped<IPrescriptionsDomainEventDispatcher, LoggingPrescriptionsDomainEventDispatcher>();

        return services;
    }

    public static async Task MigratePrescriptionsModuleAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PrescriptionsDbContext>();
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync(cancellationToken);
        else
            await db.Database.EnsureCreatedAsync(cancellationToken);
    }
}
