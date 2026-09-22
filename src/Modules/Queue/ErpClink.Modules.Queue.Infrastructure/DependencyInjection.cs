using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Options;
using ErpClink.BuildingBlocks.Infrastructure.Organization;
using ErpClink.BuildingBlocks.Infrastructure.Time;
using ErpClink.Modules.Queue.Application.Common;
using ErpClink.Modules.Queue.Application.Contracts;
using ErpClink.Modules.Queue.Application.Entries;
using ErpClink.Modules.Queue.Application.Validators;
using ErpClink.Modules.Queue.Infrastructure.Contracts;
using ErpClink.Modules.Queue.Infrastructure.Entries;
using ErpClink.Modules.Queue.Infrastructure.Events;
using ErpClink.Modules.Queue.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ErpClink.Modules.Queue.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddQueueModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrganizationOptions>(configuration.GetSection(OrganizationOptions.SectionName));
        services.TryAddSingleton<IOrganizationContext, ConfigurationOrganizationContext>();
        services.TryAddSingleton<IBusinessClock, UtcBusinessClock>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<QueueDbContext>(options => options.UseSqlServer(connectionString));

        services.AddValidatorsFromAssemblyContaining<CheckInRequestValidator>();
        services.AddScoped<IQueueNumberGenerator, SequentialQueueNumberGenerator>();
        services.AddScoped<IQueueService, QueueService>();
        services.AddScoped<IQueueVisitPort, QueueVisitPort>();
        services.AddSingleton<QueueDomainEventCollector>();
        services.AddScoped<IQueueDomainEventDispatcher, LoggingQueueDomainEventDispatcher>();

        return services;
    }

    public static async Task MigrateQueueModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QueueDbContext>();
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync(cancellationToken);
        else
            await db.Database.EnsureCreatedAsync(cancellationToken);
    }
}
