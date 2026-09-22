using ErpClink.Modules.Assets.Domain.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpClink.Modules.Assets.Infrastructure.Persistence.Configurations;

public sealed class AssetFinancialProfileConfiguration : IEntityTypeConfiguration<AssetFinancialProfile>
{
    public void Configure(EntityTypeBuilder<AssetFinancialProfile> builder)
    {
        builder.ToTable("AssetFinancialProfiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AcquisitionCost).HasPrecision(18, 2);
        builder.Property(x => x.CapitalizedCost).HasPrecision(18, 2);
        builder.Property(x => x.ResidualValue).HasPrecision(18, 2);
        builder.Property(x => x.AccumulatedDepreciation).HasPrecision(18, 2);
        builder.Property(x => x.NetBookValue).HasPrecision(18, 2);
        builder.Property(x => x.DisposalProceeds).HasPrecision(18, 2);
        builder.Property(x => x.DisposalGainLoss).HasPrecision(18, 2);
        builder.Property(x => x.DisposalNotes).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.OrganizationId, x.AssetId }).IsUnique()
            .HasDatabaseName("UX_AssetFinancialProfiles_Org_Asset");
        builder.HasIndex(x => new { x.OrganizationId, x.BranchId, x.Status })
            .HasDatabaseName("IX_AssetFinancialProfiles_Org_Branch_Status");
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_AssetFinancialProfiles_Costs",
                "[AcquisitionCost] >= 0 AND [CapitalizedCost] >= 0 AND [ResidualValue] >= 0 AND [ResidualValue] <= [CapitalizedCost]");
            t.HasCheckConstraint("CK_AssetFinancialProfiles_Accum",
                "[AccumulatedDepreciation] >= 0 AND [NetBookValue] >= 0 AND [UsefulLifeMonths] > 0");
        });
        builder.Ignore(x => x.DepreciableBase);
        builder.Ignore(x => x.RemainingDepreciableAmount);
    }
}

public sealed class AssetDepreciationScheduleLineConfiguration : IEntityTypeConfiguration<AssetDepreciationScheduleLine>
{
    public void Configure(EntityTypeBuilder<AssetDepreciationScheduleLine> builder)
    {
        builder.ToTable("AssetDepreciationScheduleLines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PeriodKey).HasMaxLength(7).IsRequired();
        builder.Property(x => x.PlannedAmount).HasPrecision(18, 2);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.OrganizationId, x.AssetId, x.PeriodKey }).IsUnique()
            .HasDatabaseName("UX_AssetDepreciationSchedule_Org_Asset_Period");
        builder.HasIndex(x => new { x.OrganizationId, x.AssetId, x.Status, x.PeriodIndex })
            .HasDatabaseName("IX_AssetDepreciationSchedule_Open");
        builder.ToTable(t => t.HasCheckConstraint("CK_AssetDepreciationSchedule_Planned", "[PlannedAmount] >= 0"));
    }
}

public sealed class AssetDepreciationTransactionConfiguration : IEntityTypeConfiguration<AssetDepreciationTransaction>
{
    public void Configure(EntityTypeBuilder<AssetDepreciationTransaction> builder)
    {
        builder.ToTable("AssetDepreciationTransactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PeriodKey).HasMaxLength(7).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.OpeningNetBookValue).HasPrecision(18, 2);
        builder.Property(x => x.DepreciationAmount).HasPrecision(18, 2);
        builder.Property(x => x.ClosingAccumulatedDepreciation).HasPrecision(18, 2);
        builder.Property(x => x.ClosingNetBookValue).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.OrganizationId, x.IdempotencyKey }).IsUnique()
            .HasDatabaseName("UX_AssetDepreciationTransactions_Idempotency");
        builder.HasIndex(x => new { x.OrganizationId, x.AssetId, x.PeriodKey }).IsUnique()
            .HasDatabaseName("UX_AssetDepreciationTransactions_Org_Asset_Period");
        builder.HasIndex(x => new { x.OrganizationId, x.AssetId, x.PeriodStartDate })
            .HasDatabaseName("IX_AssetDepreciationTransactions_Asset_Period");
        builder.ToTable(t => t.HasCheckConstraint("CK_AssetDepreciationTransactions_Amounts",
            "[DepreciationAmount] > 0 AND [OpeningNetBookValue] >= 0 AND [ClosingNetBookValue] >= 0 AND [ClosingAccumulatedDepreciation] >= 0"));
    }
}

public sealed class AssetFinancialTransactionConfiguration : IEntityTypeConfiguration<AssetFinancialTransaction>
{
    public void Configure(EntityTypeBuilder<AssetFinancialTransaction> builder)
    {
        builder.ToTable("AssetFinancialTransactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SourceType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.OpeningNetBookValue).HasPrecision(18, 2);
        builder.Property(x => x.ClosingNetBookValue).HasPrecision(18, 2);
        builder.Property(x => x.AccumulatedDepreciation).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.OrganizationId, x.IdempotencyKey }).IsUnique()
            .HasDatabaseName("UX_AssetFinancialTransactions_Idempotency");
        builder.HasIndex(x => new { x.OrganizationId, x.AssetId, x.TransactionDate })
            .HasDatabaseName("IX_AssetFinancialTransactions_Asset_Date");
        builder.ToTable(t => t.HasCheckConstraint("CK_AssetFinancialTransactions_Amount", "[Amount] >= 0"));
    }
}
