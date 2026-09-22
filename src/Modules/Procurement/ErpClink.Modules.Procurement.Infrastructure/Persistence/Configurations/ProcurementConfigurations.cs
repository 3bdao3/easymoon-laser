using ErpClink.Modules.Procurement.Domain.PurchaseOrders;
using ErpClink.Modules.Procurement.Domain.Suppliers;
using ErpClink.Modules.Procurement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpClink.Modules.Procurement.Infrastructure.Persistence.Configurations;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.SupplierCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContactName).HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(32);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.OrganizationId, x.SupplierCode }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.Name });
        builder.HasIndex(x => new { x.OrganizationId, x.IsActive });
    }
}

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.PurchaseOrderNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.SubTotal).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.SubmittedBy).HasMaxLength(64);
        builder.Property(x => x.ApprovedBy).HasMaxLength(64);
        builder.Property(x => x.CancelledBy).HasMaxLength(64);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => new { x.OrganizationId, x.PurchaseOrderNumber }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.SupplierId, x.OrderDate });
        builder.HasIndex(x => new { x.OrganizationId, x.Status, x.OrderDate });
        builder.HasIndex(x => x.BranchId);
    }
}

public sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DescriptionSnapshot).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.QuantityReceived).HasPrecision(18, 3).HasDefaultValue(0m);
        builder.Property(x => x.UnitCost).HasPrecision(18, 2);
        builder.Property(x => x.LineDiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.LineSubtotal).HasPrecision(18, 2);
        builder.Property(x => x.LineTotal).HasPrecision(18, 2);

        builder.HasIndex(x => x.PurchaseOrderId);
        builder.HasIndex(x => x.CatalogItemId);
    }
}

public sealed class SupplierNumberSequenceConfiguration : IEntityTypeConfiguration<SupplierNumberSequence>
{
    public void Configure(EntityTypeBuilder<SupplierNumberSequence> builder)
    {
        builder.ToTable("SupplierNumberSequences");
        builder.HasKey(x => new { x.OrganizationId, x.Year });
    }
}

public sealed class PurchaseOrderNumberSequenceConfiguration : IEntityTypeConfiguration<PurchaseOrderNumberSequence>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderNumberSequence> builder)
    {
        builder.ToTable("PurchaseOrderNumberSequences");
        builder.HasKey(x => new { x.OrganizationId, x.Year });
    }
}
