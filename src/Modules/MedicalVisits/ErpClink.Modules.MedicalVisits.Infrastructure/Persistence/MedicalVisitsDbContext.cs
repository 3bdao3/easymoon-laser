using ErpClink.Modules.MedicalVisits.Domain.Visits;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.MedicalVisits.Infrastructure.Persistence;

public sealed class VisitNumberSequence
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public long LastValue { get; set; }
}

public sealed class MedicalVisitsDbContext : DbContext
{
    public MedicalVisitsDbContext(DbContextOptions<MedicalVisitsDbContext> options) : base(options)
    {
    }

    public DbSet<MedicalVisit> MedicalVisits => Set<MedicalVisit>();
    public DbSet<VisitNumberSequence> VisitNumberSequences => Set<VisitNumberSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("medical_visits");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MedicalVisitsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
