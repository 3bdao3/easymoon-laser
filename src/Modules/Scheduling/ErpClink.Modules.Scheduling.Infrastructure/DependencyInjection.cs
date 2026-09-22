using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Options;
using ErpClink.BuildingBlocks.Infrastructure.Organization;
using ErpClink.Modules.Scheduling.Application.Common;
using ErpClink.Modules.Scheduling.Application.Schedules;
using ErpClink.Modules.Scheduling.Application.Validators;
using ErpClink.Modules.Scheduling.Infrastructure.Events;
using ErpClink.Modules.Scheduling.Infrastructure.Persistence;
using ErpClink.Modules.Scheduling.Infrastructure.Schedules;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ErpClink.Modules.Scheduling.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSchedulingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrganizationOptions>(configuration.GetSection(OrganizationOptions.SectionName));
        services.TryAddSingleton<IOrganizationContext, ConfigurationOrganizationContext>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<SchedulingDbContext>(options => options.UseSqlServer(connectionString));

        services.AddValidatorsFromAssemblyContaining<CreateDoctorScheduleRequestValidator>();
        services.AddScoped<IDoctorScheduleService, DoctorScheduleService>();
        services.AddScoped<IScheduleAvailabilityService, ScheduleAvailabilityService>();
        services.AddSingleton<SchedulingDomainEventCollector>();
        services.AddScoped<ISchedulingDomainEventDispatcher, LoggingSchedulingDomainEventDispatcher>();

        return services;
    }

    public static async Task MigrateSchedulingModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync(cancellationToken);
        else
            await db.Database.EnsureCreatedAsync(cancellationToken);
    }
}
