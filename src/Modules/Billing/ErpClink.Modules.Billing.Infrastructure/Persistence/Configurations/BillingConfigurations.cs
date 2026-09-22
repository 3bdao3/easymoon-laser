using ErpClink.Modules.Billing.Domain.Invoices;
using ErpClink.Modules.Billing.Domain.Payments;
using ErpClink.Modules.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpClink.Modules.Billing.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.InvoiceNumber).HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.SubTotal).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.PaidAmount).HasPrecision(18, 2);
        builder.Property(x => x.OutstandingAmount).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.IssuedBy).HasMaxLength(64);
        builder.Property(x => x.VoidedBy).HasMaxLength(64);
        builder.Property(x => x.VoidReason).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => new { x.OrganizationId, x.InvoiceNumber })
            .IsUnique()
            .HasFilter("[InvoiceNumber] IS NOT NULL");

        builder.HasIndex(x => new { x.OrganizationId, x.PatientId, x.InvoiceDate });
        builder.HasIndex(x => new { x.OrganizationId, x.Status, x.InvoiceDate });
        builder.HasIndex(x => x.MedicalVisitId);
        builder.HasIndex(x => x.BranchId);
    }
}

public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.DescriptionSnapshot).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ServiceCodeSnapshot).HasMaxLength(64);
        builder.Property(x => x.Quantity).HasPrecision(18, 2);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.LineDiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.LineSubtotal).HasPrecision(18, 2);
        builder.Property(x => x.LineTotal).HasPrecision(18, 2);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();

        builder.HasIndex(x => x.InvoiceId);
        builder.HasIndex(x => x.ServiceId);
        builder.HasIndex(x => x.PackageId);
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.PaymentNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Method).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ReferenceNumber).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.ReversedBy).HasMaxLength(64);
        builder.Property(x => x.ReversalReason).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.OrganizationId, x.PaymentNumber }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.InvoiceId, x.PaymentDate });
        builder.HasIndex(x => x.BranchId);
    }
}

public sealed class InvoiceNumberSequenceConfiguration : IEntityTypeConfiguration<InvoiceNumberSequence>
{
    public void Configure(EntityTypeBuilder<InvoiceNumberSequence> builder)
    {
        builder.ToTable("InvoiceNumberSequences");
        builder.HasKey(x => new { x.OrganizationId, x.Year });
    }
}

public sealed class PaymentNumberSequenceConfiguration : IEntityTypeConfiguration<PaymentNumberSequence>
{
    public void Configure(EntityTypeBuilder<PaymentNumberSequence> builder)
    {
        builder.ToTable("PaymentNumberSequences");
        builder.HasKey(x => new { x.OrganizationId, x.Year });
    }
}
