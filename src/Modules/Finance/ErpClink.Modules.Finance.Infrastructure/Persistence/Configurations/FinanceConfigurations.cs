using ErpClink.Modules.Finance.Domain.Accounts;
using ErpClink.Modules.Finance.Domain.FiscalPeriods;
using ErpClink.Modules.Finance.Domain.FiscalYears;
using ErpClink.Modules.Finance.Domain.Integration;
using ErpClink.Modules.Finance.Domain.Journals;
using ErpClink.Modules.Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpClink.Modules.Finance.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.AccountType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
        builder.HasIndex(x => x.ParentAccountId);
    }
}

public sealed class FiscalYearConfiguration : IEntityTypeConfiguration<FiscalYear>
{
    public void Configure(EntityTypeBuilder<FiscalYear> builder)
    {
        builder.ToTable("FiscalYears");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.OrganizationId, x.StartDate, x.EndDate });
    }
}

public sealed class FiscalPeriodConfiguration : IEntityTypeConfiguration<FiscalPeriod>
{
    public void Configure(EntityTypeBuilder<FiscalPeriod> builder)
    {
        builder.ToTable("FiscalPeriods");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.OrganizationId, x.FiscalYearId, x.StartDate, x.EndDate });
        builder.HasIndex(x => x.FiscalYearId);
    }
}

public sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.JournalNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.TotalDebit).HasPrecision(18, 2);
        builder.Property(x => x.TotalCredit).HasPrecision(18, 2);
        builder.Property(x => x.PostedBy).HasMaxLength(64);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => new { x.OrganizationId, x.JournalNumber }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.JournalDate, x.Status });
        builder.HasIndex(x => x.BranchId);
        builder.HasIndex(x => x.FiscalPeriodId);
    }
}

public sealed class JournalEntryLineConfiguration : IEntityTypeConfiguration<JournalEntryLine>
{
    public void Configure(EntityTypeBuilder<JournalEntryLine> builder)
    {
        builder.ToTable("JournalEntryLines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Debit).HasPrecision(18, 2);
        builder.Property(x => x.Credit).HasPrecision(18, 2);

        builder.HasIndex(x => x.JournalEntryId);
        builder.HasIndex(x => x.AccountId);
    }
}

public sealed class JournalEntryHistoryConfiguration : IEntityTypeConfiguration<JournalEntryHistory>
{
    public void Configure(EntityTypeBuilder<JournalEntryHistory> builder)
    {
        builder.ToTable("JournalEntryHistory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.OccurredBy).HasMaxLength(64);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => x.JournalEntryId);
    }
}

public sealed class JournalNumberSequenceConfiguration : IEntityTypeConfiguration<JournalNumberSequence>
{
    public void Configure(EntityTypeBuilder<JournalNumberSequence> builder)
    {
        builder.ToTable("JournalNumberSequences");
        builder.HasKey(x => new { x.OrganizationId, x.Year });
    }
}

public sealed class AccountingIntegrationRequestConfiguration : IEntityTypeConfiguration<AccountingIntegrationRequest>
{
    public void Configure(EntityTypeBuilder<AccountingIntegrationRequest> builder)
    {
        builder.ToTable("AccountingIntegrationRequests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceModule).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SourceType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SourceId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.ErrorCode).HasMaxLength(128);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.RowVersion).IsRowVersion();

        // Org-scoped uniqueness for concurrent-safe idempotency
        builder.HasIndex(x => new
            {
                x.OrganizationId,
                x.SourceModule,
                x.SourceType,
                x.SourceId,
                x.EventType,
                x.IdempotencyKey
            })
            .IsUnique()
            .HasDatabaseName("UX_AccountingIntegrationRequests_Org_Source_Event_Key");

        builder.HasIndex(x => new { x.OrganizationId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UX_AccountingIntegrationRequests_Org_IdempotencyKey");

        builder.HasIndex(x => x.OrganizationId)
            .HasDatabaseName("IX_AccountingIntegrationRequests_OrganizationId");

        builder.HasIndex(x => new { x.OrganizationId, x.SourceModule, x.SourceType, x.SourceId })
            .HasDatabaseName("IX_AccountingIntegrationRequests_Org_SourceLookup");

        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_AccountingIntegrationRequests_CorrelationId");
    }
}
