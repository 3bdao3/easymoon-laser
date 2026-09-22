using ErpClink.Modules.Billing.Domain.Invoices;
using ErpClink.Modules.Billing.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Billing.Infrastructure.Persistence;

public sealed class InvoiceNumberSequence
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public long LastValue { get; set; }
}

public sealed class PaymentNumberSequence
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public long LastValue { get; set; }
}

public sealed class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options)
    {
    }

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<InvoiceNumberSequence> InvoiceNumberSequences => Set<InvoiceNumberSequence>();
    public DbSet<PaymentNumberSequence> PaymentNumberSequences => Set<PaymentNumberSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("billing");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
