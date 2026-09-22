using ErpClink.Modules.Inventory.Domain.Categories;
using ErpClink.Modules.Inventory.Domain.GoodsReceipts;
using ErpClink.Modules.Inventory.Domain.Items;
using ErpClink.Modules.Inventory.Domain.Stock;
using ErpClink.Modules.Inventory.Domain.Warehouses;
using ErpClink.Modules.Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpClink.Modules.Inventory.Infrastructure.Persistence.Configurations;

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.WarehouseCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.OrganizationId, x.WarehouseCode }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.BranchId, x.IsActive });
    }
}

public sealed class InventoryCategoryConfiguration : IEntityTypeConfiguration<InventoryCategory>
{
    public void Configure(EntityTypeBuilder<InventoryCategory> builder)
    {
        builder.ToTable("InventoryCategories");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
    }
}

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.ItemCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.UnitOfMeasure).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Barcode).HasMaxLength(64);
        builder.Property(x => x.MinStockQuantity).HasPrecision(18, 3);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.OrganizationId, x.ItemCode }).IsUnique();
        builder.HasIndex(x => x.CategoryId);
    }
}

public sealed class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.ToTable("StockBalances");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.InventoryValue).HasPrecision(18, 2);
        builder.Ignore(x => x.AverageUnitCost);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.OrganizationId, x.WarehouseId, x.InventoryItemId }).IsUnique();
    }
}

public sealed class StockBatchConfiguration : IEntityTypeConfiguration<StockBatch>
{
    public void Configure(EntityTypeBuilder<StockBatch> builder)
    {
        builder.ToTable("StockBatches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BatchNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.OrganizationId, x.WarehouseId, x.InventoryItemId, x.BatchNumber }).IsUnique();
    }
}

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 3);
        builder.Property(x => x.ReferenceType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.MovementType).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => new { x.OrganizationId, x.WarehouseId, x.InventoryItemId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId });
    }
}

public sealed class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
    {
        builder.ToTable("GoodsReceipts");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.ReceiptNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.PostedBy).HasMaxLength(64);
        builder.Property(x => x.CancelledBy).HasMaxLength(64);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => new { x.OrganizationId, x.ReceiptNumber }).IsUnique();
        builder.HasIndex(x => x.PurchaseOrderId);
    }
}

public sealed class GoodsReceiptLineConfiguration : IEntityTypeConfiguration<GoodsReceiptLine>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptLine> builder)
    {
        builder.ToTable("GoodsReceiptLines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DescriptionSnapshot).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.UnitCostSnapshot).HasPrecision(18, 2);
        builder.Property(x => x.BatchNumber).HasMaxLength(64);
    }
}

public sealed class WarehouseNumberSequenceConfiguration : IEntityTypeConfiguration<WarehouseNumberSequence>
{
    public void Configure(EntityTypeBuilder<WarehouseNumberSequence> builder)
    {
        builder.ToTable("WarehouseNumberSequences");
        builder.HasKey(x => new { x.OrganizationId, x.Year });
    }
}

public sealed class InventoryItemNumberSequenceConfiguration : IEntityTypeConfiguration<InventoryItemNumberSequence>
{
    public void Configure(EntityTypeBuilder<InventoryItemNumberSequence> builder)
    {
        builder.ToTable("InventoryItemNumberSequences");
        builder.HasKey(x => new { x.OrganizationId, x.Year });
    }
}

public sealed class GoodsReceiptNumberSequenceConfiguration : IEntityTypeConfiguration<GoodsReceiptNumberSequence>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptNumberSequence> builder)
    {
        builder.ToTable("GoodsReceiptNumberSequences");
        builder.HasKey(x => new { x.OrganizationId, x.Year });
    }
}
