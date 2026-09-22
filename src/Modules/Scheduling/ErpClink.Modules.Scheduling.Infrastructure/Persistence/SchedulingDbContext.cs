using ErpClink.Modules.Scheduling.Domain.Exceptions;
using ErpClink.Modules.Scheduling.Domain.Holidays;
using ErpClink.Modules.Scheduling.Domain.Schedules;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Scheduling.Infrastructure.Persistence;

public sealed class SchedulingDbContext : DbContext
{
    public SchedulingDbContext(DbContextOptions<SchedulingDbContext> options) : base(options)
    {
    }

    public DbSet<DoctorWorkingSchedule> DoctorWorkingSchedules => Set<DoctorWorkingSchedule>();
    public DbSet<ClinicHoliday> ClinicHolidays => Set<ClinicHoliday>();
    public DbSet<DoctorScheduleException> DoctorScheduleExceptions => Set<DoctorScheduleException>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("scheduling");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
