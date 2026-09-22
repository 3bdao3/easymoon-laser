using ErpClink.Modules.Finance.Domain.Subledgers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpClink.Modules.Finance.Infrastructure.Persistence.Configurations;

public sealed class SubledgerTransactionConfiguration : IEntityTypeConfiguration<SubledgerTransaction>
{
    public void Configure(EntityTypeBuilder<SubledgerTransaction> builder)
    {
        builder.ToTable("SubledgerTransactions");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.SignedBalanceImpact);
        builder.Ignore(x => x.DebitAmount);
        builder.Ignore(x => x.CreditAmount);

        builder.Property(x => x.SourceModule).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SourceType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SourceId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.SubledgerType).HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.PartyDisplayNameSnapshot).HasMaxLength(200);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PostedBy).HasMaxLength(64);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();

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
            .HasDatabaseName("UX_SubledgerTransactions_Org_Source_Event_Key");

        builder.HasIndex(x => new { x.OrganizationId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UX_SubledgerTransactions_Org_IdempotencyKey");

        builder.HasIndex(x => new { x.OrganizationId, x.SubledgerType, x.PartyId, x.TransactionDate })
            .HasDatabaseName("IX_SubledgerTransactions_Org_Type_Party_Date");

        builder.HasIndex(x => new { x.OrganizationId, x.SubledgerType, x.TransactionDate })
            .HasDatabaseName("IX_SubledgerTransactions_Org_Type_Date");

        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_SubledgerTransactions_CorrelationId");
    }
}

public sealed class SubledgerPartyBalanceConfiguration : IEntityTypeConfiguration<SubledgerPartyBalance>
{
    public void Configure(EntityTypeBuilder<SubledgerPartyBalance> builder)
    {
        builder.ToTable("SubledgerPartyBalances");
        builder.HasKey(x => new { x.OrganizationId, x.SubledgerType, x.PartyId });

        builder.Property(x => x.SubledgerType).HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.Property(x => x.Balance).HasPrecision(18, 2);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.OrganizationId, x.SubledgerType })
            .HasDatabaseName("IX_SubledgerPartyBalances_Org_Type");
    }
}
