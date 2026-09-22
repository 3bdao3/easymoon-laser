using ErpClink.Modules.Inventory.Domain.Costing;
using ErpClink.Modules.Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpClink.Modules.Inventory.Infrastructure.Persistence.Configurations;

public sealed class InventoryCostLayerConfiguration : IEntityTypeConfiguration<InventoryCostLayer>
{
    public void Configure(EntityTypeBuilder<InventoryCostLayer> builder)
    {
        builder.ToTable("InventoryCostLayers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OriginalQuantity).HasPrecision(18, 3);
        builder.Property(x => x.RemainingQuantity).HasPrecision(18, 3);
        builder.Property(x => x.UnitCost).HasPrecision(18, 6);
        builder.Property(x => x.OriginalValue).HasPrecision(18, 2);
        builder.Property(x => x.RemainingValue).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.OrganizationId, x.WarehouseId, x.InventoryItemId, x.Status, x.ReceiptDate })
            .HasDatabaseName("IX_InventoryCostLayers_OpenFifo");
        builder.HasIndex(x => new { x.OrganizationId, x.SourceType, x.SourceId })
            .HasDatabaseName("IX_InventoryCostLayers_Source");
        builder.HasIndex(x => x.StockBatchId);
    }
}

public sealed class InventoryCostTransactionConfiguration : IEntityTypeConfiguration<InventoryCostTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryCostTransaction> builder)
    {
        builder.ToTable("InventoryCostTransactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceModule).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SourceType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.UnitCost).HasPrecision(18, 6);
        builder.Property(x => x.TotalCost).HasPrecision(18, 2);
        builder.Property(x => x.TransactionType).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.ValuationMethod).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasMany(x => x.Allocations)
            .WithOne()
            .HasForeignKey(x => x.CostTransactionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Allocations).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => new
            {
                x.OrganizationId,
                x.SourceType,
                x.SourceId,
                x.EventType,
                x.IdempotencyKey
            })
            .IsUnique()
            .HasDatabaseName("UX_InventoryCostTransactions_Org_Source_Event_Key");

        builder.HasIndex(x => new { x.OrganizationId, x.InventoryItemId, x.TransactionDate })
            .HasDatabaseName("IX_InventoryCostTransactions_Item_Date");
        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => x.StockMovementId);
    }
}

public sealed class InventoryCostAllocationConfiguration : IEntityTypeConfiguration<InventoryCostAllocation>
{
    public void Configure(EntityTypeBuilder<InventoryCostAllocation> builder)
    {
        builder.ToTable("InventoryCostAllocations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.UnitCost).HasPrecision(18, 6);
        builder.Property(x => x.TotalCost).HasPrecision(18, 2);
        builder.HasIndex(x => x.CostTransactionId);
        builder.HasIndex(x => x.CostLayerId);
    }
}
