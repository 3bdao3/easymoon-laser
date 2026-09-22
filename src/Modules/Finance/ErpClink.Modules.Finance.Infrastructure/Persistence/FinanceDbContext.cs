using ErpClink.Modules.Finance.Domain.Accounts;
using ErpClink.Modules.Finance.Domain.FiscalPeriods;
using ErpClink.Modules.Finance.Domain.FiscalYears;
using ErpClink.Modules.Finance.Domain.Integration;
using ErpClink.Modules.Finance.Domain.Journals;
using ErpClink.Modules.Finance.Domain.Subledgers;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Finance.Infrastructure.Persistence;

public sealed class JournalNumberSequence
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public long LastValue { get; set; }
}

public sealed class FinanceDbContext : DbContext
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<JournalEntryHistory> JournalEntryHistory => Set<JournalEntryHistory>();
    public DbSet<JournalNumberSequence> JournalNumberSequences => Set<JournalNumberSequence>();
    public DbSet<AccountingIntegrationRequest> AccountingIntegrationRequests => Set<AccountingIntegrationRequest>();
    public DbSet<SubledgerTransaction> SubledgerTransactions => Set<SubledgerTransaction>();
    public DbSet<SubledgerPartyBalance> SubledgerPartyBalances => Set<SubledgerPartyBalance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("finance");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinanceDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
