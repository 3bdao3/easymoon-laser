using ErpClink.Modules.Queue.Domain.Entries;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Queue.Infrastructure.Persistence;

public sealed class QueueNumberSequence
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ClinicId { get; set; }
    public DateOnly QueueDate { get; set; }
    public long LastValue { get; set; }
}

public sealed class QueueDbContext : DbContext
{
    public QueueDbContext(DbContextOptions<QueueDbContext> options) : base(options)
    {
    }

    public DbSet<QueueEntry> QueueEntries => Set<QueueEntry>();
    public DbSet<QueueNumberSequence> QueueNumberSequences => Set<QueueNumberSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("queue");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QueueDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
