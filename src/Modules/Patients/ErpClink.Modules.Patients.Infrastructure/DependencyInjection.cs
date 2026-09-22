using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Options;
using ErpClink.BuildingBlocks.Infrastructure;
using ErpClink.BuildingBlocks.Infrastructure.Organization;
using ErpClink.Modules.Patients.Application.Common;
using ErpClink.Modules.Patients.Application.Patients;
using ErpClink.Modules.Patients.Application.Patients.Validators;
using ErpClink.Modules.Patients.Infrastructure.Events;
using ErpClink.Modules.Patients.Infrastructure.Patients;
using ErpClink.Modules.Patients.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Modules.Patients.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPatientsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrganizationOptions>(configuration.GetSection(OrganizationOptions.SectionName));
        services.AddSingleton<IOrganizationContext, ConfigurationOrganizationContext>();
        services.AddLocalFileStorage(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<PatientsDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddValidatorsFromAssemblyContaining<RegisterPatientRequestValidator>();
        services.AddScoped<IPatientNumberGenerator, SequentialPatientNumberGenerator>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IPatientDocumentService, PatientDocumentService>();
        services.AddScoped<ErpClink.Modules.Patients.Application.Contracts.IPatientLookup, Contracts.PatientLookup>();
        services.AddSingleton<DomainEventCollector>();
        services.AddScoped<IDomainEventDispatcher, LoggingDomainEventDispatcher>();

        return services;
    }

    public static async Task MigratePatientsModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatientsDbContext>();
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }
    }
}
