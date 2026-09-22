using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Options;
using ErpClink.BuildingBlocks.Infrastructure.Organization;
using ErpClink.BuildingBlocks.Infrastructure.Time;
using ErpClink.Modules.Inventory.Application.Categories;
using ErpClink.Modules.Inventory.Application.Common;
using ErpClink.Modules.Inventory.Application.Contracts;
using ErpClink.Modules.Inventory.Application.Costing;
using ErpClink.Modules.Inventory.Application.Costing.Models;
using ErpClink.Modules.Inventory.Application.GoodsReceipts;
using ErpClink.Modules.Inventory.Application.Items;
using ErpClink.Modules.Inventory.Application.Stock;
using ErpClink.Modules.Inventory.Application.Validators;
using ErpClink.Modules.Inventory.Application.Warehouses;
using ErpClink.Modules.Inventory.Infrastructure.Categories;
using ErpClink.Modules.Inventory.Infrastructure.Contracts;
using ErpClink.Modules.Inventory.Infrastructure.Costing;
using ErpClink.Modules.Inventory.Infrastructure.Events;
using ErpClink.Modules.Inventory.Infrastructure.GoodsReceipts;
using ErpClink.Modules.Inventory.Infrastructure.Items;
using ErpClink.Modules.Inventory.Infrastructure.Numbering;
using ErpClink.Modules.Inventory.Infrastructure.Persistence;
using ErpClink.Modules.Inventory.Infrastructure.Stock;
using ErpClink.Modules.Inventory.Infrastructure.Warehouses;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ErpClink.Modules.Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrganizationOptions>(configuration.GetSection(OrganizationOptions.SectionName));
        services.TryAddSingleton<IOrganizationContext, ConfigurationOrganizationContext>();
        services.TryAddSingleton<IBusinessClock, UtcBusinessClock>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<InventoryDbContext>(options => options.UseSqlServer(connectionString));

        services.AddValidatorsFromAssemblyContaining<CreateWarehouseRequestValidator>();

        services.AddScoped<IWarehouseNumberGenerator, SequentialWarehouseNumberGenerator>();
        services.AddScoped<IInventoryItemNumberGenerator, SequentialInventoryItemNumberGenerator>();
        services.AddScoped<IGoodsReceiptNumberGenerator, SequentialGoodsReceiptNumberGenerator>();

        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IInventoryCategoryService, InventoryCategoryService>();
        services.AddScoped<IInventoryItemService, InventoryItemService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
        services.AddScoped<IInventoryValuationMethod, FifoInventoryValuationMethod>();
        services.AddScoped<IInventoryValuationQueryService, InventoryValuationQueryService>();

        services.AddScoped<IWarehouseLookup, WarehouseLookup>();
        services.AddScoped<IInventoryItemLookup, InventoryItemLookup>();
        services.AddScoped<IStockLookup, StockLookup>();
        services.AddScoped<IGoodsReceiptLookup, GoodsReceiptLookup>();

        services.AddSingleton<InventoryDomainEventCollector>();
        services.AddScoped<IInventoryDomainEventDispatcher, LoggingInventoryDomainEventDispatcher>();

        return services;
    }

    public static async Task MigrateInventoryModuleAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync(cancellationToken);
        else
            await db.Database.EnsureCreatedAsync(cancellationToken);
    }
}
