using ErpClink.Modules.Procurement.Domain.PurchaseOrders;
using ErpClink.Modules.Procurement.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Procurement.Infrastructure.Persistence;

public sealed class SupplierNumberSequence
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public long LastValue { get; set; }
}

public sealed class PurchaseOrderNumberSequence
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public long LastValue { get; set; }
}

public sealed class ProcurementDbContext : DbContext
{
    public ProcurementDbContext(DbContextOptions<ProcurementDbContext> options) : base(options)
    {
    }

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<SupplierNumberSequence> SupplierNumberSequences => Set<SupplierNumberSequence>();
    public DbSet<PurchaseOrderNumberSequence> PurchaseOrderNumberSequences => Set<PurchaseOrderNumberSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("procurement");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProcurementDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
