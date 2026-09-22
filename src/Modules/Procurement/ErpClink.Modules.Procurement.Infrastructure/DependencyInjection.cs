using ErpClink.BuildingBlocks.Application.Abstractions;

using ErpClink.BuildingBlocks.Application.Options;

using ErpClink.BuildingBlocks.Infrastructure.Organization;

using ErpClink.BuildingBlocks.Infrastructure.Time;

using ErpClink.Modules.Procurement.Application.Common;

using ErpClink.Modules.Procurement.Application.Contracts;

using ErpClink.Modules.Procurement.Application.PurchaseOrders;

using ErpClink.Modules.Procurement.Application.Suppliers;

using ErpClink.Modules.Procurement.Application.Validators;

using ErpClink.Modules.Procurement.Infrastructure.Contracts;

using ErpClink.Modules.Procurement.Infrastructure.Events;

using ErpClink.Modules.Procurement.Infrastructure.Persistence;

using ErpClink.Modules.Procurement.Infrastructure.PurchaseOrders;

using ErpClink.Modules.Procurement.Infrastructure.Suppliers;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.DependencyInjection.Extensions;



namespace ErpClink.Modules.Procurement.Infrastructure;



public static class DependencyInjection

{

    public static IServiceCollection AddProcurementModule(this IServiceCollection services, IConfiguration configuration)

    {

        services.Configure<OrganizationOptions>(configuration.GetSection(OrganizationOptions.SectionName));

        services.TryAddSingleton<IOrganizationContext, ConfigurationOrganizationContext>();

        services.TryAddSingleton<IBusinessClock, UtcBusinessClock>();



        var connectionString = configuration.GetConnectionString("DefaultConnection")

            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");



        services.AddDbContext<ProcurementDbContext>(options => options.UseSqlServer(connectionString));



        services.AddValidatorsFromAssemblyContaining<CreateSupplierRequestValidator>();

        services.AddScoped<ISupplierNumberGenerator, SequentialSupplierNumberGenerator>();

        services.AddScoped<IPurchaseOrderNumberGenerator, SequentialPurchaseOrderNumberGenerator>();

        services.AddScoped<ISupplierService, SupplierService>();

        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();

        services.AddScoped<ISupplierLookup, SupplierLookup>();

        services.AddScoped<IPurchaseOrderLookup, PurchaseOrderLookup>();

        services.AddScoped<IPurchaseOrderReceivingPort, PurchaseOrderReceivingPort>();

        services.AddSingleton<ProcurementDomainEventCollector>();

        services.AddScoped<IProcurementDomainEventDispatcher, LoggingProcurementDomainEventDispatcher>();



        return services;

    }



    public static async Task MigrateProcurementModuleAsync(

        this IServiceProvider services,

        CancellationToken cancellationToken = default)

    {

        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();

        if (db.Database.IsRelational())

            await db.Database.MigrateAsync(cancellationToken);

        else

            await db.Database.EnsureCreatedAsync(cancellationToken);

    }

}

