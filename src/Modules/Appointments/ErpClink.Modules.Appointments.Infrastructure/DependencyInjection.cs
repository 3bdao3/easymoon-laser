using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Options;
using ErpClink.BuildingBlocks.Infrastructure.Organization;
using ErpClink.BuildingBlocks.Infrastructure.Time;
using ErpClink.Modules.Appointments.Application.Appointments;
using ErpClink.Modules.Appointments.Application.Common;
using ErpClink.Modules.Appointments.Application.Contracts;
using ErpClink.Modules.Appointments.Application.Validators;
using ErpClink.Modules.Appointments.Infrastructure.Appointments;
using ErpClink.Modules.Appointments.Infrastructure.Contracts;
using ErpClink.Modules.Appointments.Infrastructure.Events;
using ErpClink.Modules.Appointments.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ErpClink.Modules.Appointments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAppointmentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrganizationOptions>(configuration.GetSection(OrganizationOptions.SectionName));
        services.TryAddSingleton<IOrganizationContext, ConfigurationOrganizationContext>();
        services.TryAddSingleton<IBusinessClock, UtcBusinessClock>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppointmentsDbContext>(options => options.UseSqlServer(connectionString));

        services.AddValidatorsFromAssemblyContaining<BookAppointmentRequestValidator>();
        services.AddScoped<IAppointmentNumberGenerator, SequentialAppointmentNumberGenerator>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IAppointmentLookup, AppointmentLookup>();
        services.AddScoped<IAppointmentCheckInPort, AppointmentCheckInPort>();
        services.AddSingleton<AppointmentsDomainEventCollector>();
        services.AddScoped<IAppointmentsDomainEventDispatcher, LoggingAppointmentsDomainEventDispatcher>();

        return services;
    }

    public static async Task MigrateAppointmentsModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppointmentsDbContext>();
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync(cancellationToken);
        else
            await db.Database.EnsureCreatedAsync(cancellationToken);
    }
}
