using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Options;
using ErpClink.BuildingBlocks.Infrastructure.Organization;
using ErpClink.BuildingBlocks.Infrastructure.Time;
using ErpClink.Modules.Billing.Application.Common;
using ErpClink.Modules.Billing.Application.Contracts;
using ErpClink.Modules.Billing.Application.Invoices;
using ErpClink.Modules.Billing.Application.Payments;
using ErpClink.Modules.Billing.Application.Validators;
using ErpClink.Modules.Billing.Infrastructure.Contracts;
using ErpClink.Modules.Billing.Infrastructure.Events;
using ErpClink.Modules.Billing.Infrastructure.FinanceIntegration;
using ErpClink.Modules.Billing.Infrastructure.Invoices;
using ErpClink.Modules.Billing.Infrastructure.Payments;
using ErpClink.Modules.Billing.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ErpClink.Modules.Billing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrganizationOptions>(configuration.GetSection(OrganizationOptions.SectionName));
        services.TryAddSingleton<IOrganizationContext, ConfigurationOrganizationContext>();
        services.TryAddSingleton<IBusinessClock, UtcBusinessClock>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<BillingDbContext>(options => options.UseSqlServer(connectionString));

        services.AddValidatorsFromAssemblyContaining<CreateDraftInvoiceRequestValidator>();
        services.AddScoped<IInvoiceNumberGenerator, SequentialInvoiceNumberGenerator>();
        services.AddScoped<IPaymentNumberGenerator, SequentialPaymentNumberGenerator>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IInvoiceLookup, InvoiceLookup>();
        services.AddScoped<IPaymentLookup, PaymentLookup>();
        services.AddScoped<BillingArIntegration>();
        services.AddSingleton<BillingDomainEventCollector>();
        services.AddScoped<IBillingDomainEventDispatcher, LoggingBillingDomainEventDispatcher>();

        return services;
    }

    public static async Task MigrateBillingModuleAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync(cancellationToken);
        else
            await db.Database.EnsureCreatedAsync(cancellationToken);
    }
}
