using ErpClink.Modules.LaserClinic.Application.Appointments;
using ErpClink.Modules.LaserClinic.Application.Customers;
using ErpClink.Modules.LaserClinic.Application.Dashboard;
using ErpClink.Modules.LaserClinic.Application.Offers;
using ErpClink.Modules.LaserClinic.Application.Services;
using ErpClink.Modules.LaserClinic.Application.Settings;
using ErpClink.Modules.LaserClinic.Infrastructure.Appointments;
using ErpClink.Modules.LaserClinic.Infrastructure.Customers;
using ErpClink.Modules.LaserClinic.Infrastructure.Dashboard;
using ErpClink.Modules.LaserClinic.Infrastructure.Offers;
using ErpClink.Modules.LaserClinic.Infrastructure.Persistence;
using ErpClink.Modules.LaserClinic.Infrastructure.Services;
using ErpClink.Modules.LaserClinic.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Modules.LaserClinic.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLaserClinicModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<LaserClinicDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<ICustomerAppService, CustomerAppService>();
        services.AddScoped<ILaserServiceCatalogAppService, LaserServiceCatalogAppService>();
        services.AddScoped<ILaserOfferAppService, LaserOfferAppService>();
        services.AddScoped<ClinicSettingsAppService>();
        services.AddScoped<IClinicSettingsAppService>(sp => sp.GetRequiredService<ClinicSettingsAppService>());
        services.AddScoped<ILaserAppointmentAppService, LaserAppointmentAppService>();
        services.AddScoped<ILaserDashboardAppService, LaserDashboardAppService>();

        return services;
    }

    public static async Task MigrateLaserClinicModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LaserClinicDbContext>();
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync(cancellationToken);
        else
            await db.Database.EnsureCreatedAsync(cancellationToken);

        await LaserClinicDbSeeder.SeedAsync(services, cancellationToken);
    }
}
