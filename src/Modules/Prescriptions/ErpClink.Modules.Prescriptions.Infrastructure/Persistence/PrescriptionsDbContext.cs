using ErpClink.Modules.Prescriptions.Domain.Medications;
using ErpClink.Modules.Prescriptions.Domain.Prescriptions;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Prescriptions.Infrastructure.Persistence;

public sealed class PrescriptionNumberSequence
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public long LastValue { get; set; }
}

public sealed class PrescriptionsDbContext : DbContext
{
    public PrescriptionsDbContext(DbContextOptions<PrescriptionsDbContext> options) : base(options)
    {
    }

    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<PrescriptionNumberSequence> PrescriptionNumberSequences => Set<PrescriptionNumberSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("prescriptions");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrescriptionsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
