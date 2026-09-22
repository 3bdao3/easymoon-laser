using ErpClink.Modules.Assets.Domain.Assets;
using ErpClink.Modules.Assets.Domain.Categories;
using ErpClink.Modules.Assets.Domain.History;
using ErpClink.Modules.Assets.Domain.Locations;
using ErpClink.Modules.Assets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpClink.Modules.Assets.Infrastructure.Persistence.Configurations;

public sealed class AssetCategoryConfiguration : IEntityTypeConfiguration<AssetCategory>
{
    public void Configure(EntityTypeBuilder<AssetCategory> builder)
    {
        builder.ToTable("AssetCategories");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
    }
}

public sealed class AssetLocationConfiguration : IEntityTypeConfiguration<AssetLocation>
{
    public void Configure(EntityTypeBuilder<AssetLocation> builder)
    {
        builder.ToTable("AssetLocations");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.BranchId, x.IsActive });
    }
}

public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.AssetNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.SerialNumber).HasMaxLength(128);
        builder.Property(x => x.AcquisitionCost).HasPrecision(18, 2);
        builder.Property(x => x.AcquisitionReference).HasMaxLength(500);
        builder.Property(x => x.WarrantyNotes).HasMaxLength(500);
        // PurchaseDate, WarrantyStartDate, WarrantyEndDate use default date mapping.
        builder.Property(x => x.MaintenanceNotes).HasMaxLength(500);
        builder.Property(x => x.RetiredBy).HasMaxLength(64);
        builder.Property(x => x.RetirementReason).HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.OrganizationId, x.AssetNumber }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.SerialNumber })
            .IsUnique()
            .HasFilter("[SerialNumber] IS NOT NULL");
        builder.HasIndex(x => new { x.OrganizationId, x.BranchId, x.Status });
        builder.HasIndex(x => x.AssetCategoryId);
        builder.HasIndex(x => x.AssetLocationId);
    }
}

public sealed class AssetHistoryEntryConfiguration : IEntityTypeConfiguration<AssetHistoryEntry>
{
    public void Configure(EntityTypeBuilder<AssetHistoryEntry> builder)
    {
        builder.ToTable("AssetHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(500).IsRequired();
        builder.Property(x => x.OldValue).HasMaxLength(500);
        builder.Property(x => x.NewValue).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.OccurredBy).HasMaxLength(64);
        builder.HasIndex(x => new { x.AssetId, x.OccurredAtUtc });
        builder.HasIndex(x => x.OrganizationId);
    }
}

public sealed class AssetNumberSequenceConfiguration : IEntityTypeConfiguration<AssetNumberSequence>
{
    public void Configure(EntityTypeBuilder<AssetNumberSequence> builder)
    {
        builder.ToTable("AssetNumberSequences");
        builder.HasKey(x => new { x.OrganizationId, x.Year });
    }
}

public sealed class AssetCategoryNumberSequenceConfiguration : IEntityTypeConfiguration<AssetCategoryNumberSequence>
{
    public void Configure(EntityTypeBuilder<AssetCategoryNumberSequence> builder)
    {
        builder.ToTable("AssetCategoryNumberSequences");
        builder.HasKey(x => new { x.OrganizationId, x.Year });
    }
}

public sealed class AssetLocationNumberSequenceConfiguration : IEntityTypeConfiguration<AssetLocationNumberSequence>
{
    public void Configure(EntityTypeBuilder<AssetLocationNumberSequence> builder)
    {
        builder.ToTable("AssetLocationNumberSequences");
        builder.HasKey(x => new { x.OrganizationId, x.Year });
    }
}
