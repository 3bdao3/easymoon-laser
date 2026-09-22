using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.Application.Options;
using ErpClink.BuildingBlocks.Infrastructure.Organization;
using ErpClink.BuildingBlocks.Infrastructure.Time;
using ErpClink.Modules.Finance.Application.Accounts;
using ErpClink.Modules.Finance.Application.Common;
using ErpClink.Modules.Finance.Application.Contracts;
using ErpClink.Modules.Finance.Application.FiscalPeriods;
using ErpClink.Modules.Finance.Application.FiscalYears;
using ErpClink.Modules.Finance.Application.GeneralLedger;
using ErpClink.Modules.Finance.Application.Integration;
using ErpClink.Modules.Finance.Application.Journals;
using ErpClink.Modules.Finance.Application.Subledgers;
using ErpClink.Modules.Finance.Application.TrialBalance;
using ErpClink.Modules.Finance.Application.Validators;
using ErpClink.Modules.Finance.Infrastructure.Accounts;
using ErpClink.Modules.Finance.Infrastructure.Contracts;
using ErpClink.Modules.Finance.Infrastructure.Events;
using ErpClink.Modules.Finance.Infrastructure.FiscalPeriods;
using ErpClink.Modules.Finance.Infrastructure.FiscalYears;
using ErpClink.Modules.Finance.Infrastructure.GeneralLedger;
using ErpClink.Modules.Finance.Infrastructure.Integration;
using ErpClink.Modules.Finance.Infrastructure.Journals;
using ErpClink.Modules.Finance.Infrastructure.Persistence;
using ErpClink.Modules.Finance.Infrastructure.Subledgers;
using ErpClink.Modules.Finance.Infrastructure.TrialBalance;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ErpClink.Modules.Finance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFinanceModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrganizationOptions>(configuration.GetSection(OrganizationOptions.SectionName));
        services.TryAddSingleton<IOrganizationContext, ConfigurationOrganizationContext>();
        services.TryAddSingleton<IBusinessClock, UtcBusinessClock>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<FinanceDbContext>(options => options.UseSqlServer(connectionString));

        services.AddValidatorsFromAssemblyContaining<CreateAccountRequestValidator>();
        services.AddScoped<IJournalNumberGenerator, SequentialJournalNumberGenerator>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IFiscalYearService, FiscalYearService>();
        services.AddScoped<IFiscalPeriodService, FiscalPeriodService>();
        services.AddScoped<IJournalService, JournalService>();
        services.AddScoped<IGeneralLedgerService, GeneralLedgerService>();
        services.AddScoped<ITrialBalanceService, TrialBalanceService>();
        services.AddScoped<IAccountLookup, AccountLookup>();
        services.AddScoped<IAccountingPostingPort, AccountingPostingPort>();
        services.AddScoped<ISubledgerPostingPort, SubledgerPostingPort>();
        services.AddScoped<IArSubledgerQueryService, ArSubledgerQueryService>();
        services.AddScoped<IApSubledgerQueryService, ApSubledgerQueryService>();
        services.AddScoped<IArCustomerLookup, ArCustomerLookup>();
        services.AddScoped<IApSupplierLookup, ApSupplierLookupAdapter>();
        services.AddSingleton<FinanceDomainEventCollector>();
        services.AddSingleton<FinanceIntegrationEventCollector>();
        services.AddScoped<IFinanceDomainEventDispatcher, LoggingFinanceDomainEventDispatcher>();

        return services;
    }

    public static async Task MigrateFinanceModuleAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync(cancellationToken);
        else
            await db.Database.EnsureCreatedAsync(cancellationToken);
    }
}
