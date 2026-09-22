using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Options;
using ErpClink.BuildingBlocks.Infrastructure.Organization;
using ErpClink.Modules.Doctors.Application.Clinics;
using ErpClink.Modules.Doctors.Application.Common;
using ErpClink.Modules.Doctors.Application.Doctors;
using ErpClink.Modules.Doctors.Application.Specialties;
using ErpClink.Modules.Doctors.Application.Validators;
using ErpClink.Modules.Doctors.Infrastructure.Clinics;
using ErpClink.Modules.Doctors.Infrastructure.Doctors;
using ErpClink.Modules.Doctors.Infrastructure.Events;
using ErpClink.Modules.Doctors.Infrastructure.Persistence;
using ErpClink.Modules.Doctors.Infrastructure.Specialties;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ErpClink.Modules.Doctors.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDoctorsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrganizationOptions>(configuration.GetSection(OrganizationOptions.SectionName));
        services.TryAddSingleton<IOrganizationContext, ConfigurationOrganizationContext>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<DoctorsDbContext>(options => options.UseSqlServer(connectionString));

        services.AddValidatorsFromAssemblyContaining<RegisterDoctorRequestValidator>();
        services.AddScoped<IDoctorNumberGenerator, SequentialDoctorNumberGenerator>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IClinicService, ClinicService>();
        services.AddScoped<ISpecialtyService, SpecialtyService>();
        services.AddScoped<ErpClink.Modules.Doctors.Application.Contracts.IDoctorClinicLookup, Contracts.DoctorClinicLookup>();
        services.AddSingleton<DoctorsDomainEventCollector>();
        services.AddScoped<IDoctorsDomainEventDispatcher, LoggingDoctorsDomainEventDispatcher>();

        return services;
    }

    public static async Task MigrateDoctorsModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DoctorsDbContext>();
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync(cancellationToken);
        else
            await db.Database.EnsureCreatedAsync(cancellationToken);
    }
}
