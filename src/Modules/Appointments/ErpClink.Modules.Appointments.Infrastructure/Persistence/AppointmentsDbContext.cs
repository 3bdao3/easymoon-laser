using ErpClink.Modules.Appointments.Domain.Appointments;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Appointments.Infrastructure.Persistence;

public sealed class AppointmentNumberSequence
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public long LastValue { get; set; }
}

public sealed class AppointmentsDbContext : DbContext
{
    public AppointmentsDbContext(DbContextOptions<AppointmentsDbContext> options) : base(options)
    {
    }

    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentStatusHistoryEntry> AppointmentStatusHistory => Set<AppointmentStatusHistoryEntry>();
    public DbSet<AppointmentRescheduleHistoryEntry> AppointmentRescheduleHistory => Set<AppointmentRescheduleHistoryEntry>();
    public DbSet<AppointmentNumberSequence> AppointmentNumberSequences => Set<AppointmentNumberSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("appointments");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppointmentsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
