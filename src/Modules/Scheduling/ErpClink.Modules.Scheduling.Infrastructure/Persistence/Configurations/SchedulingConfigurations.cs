using ErpClink.Modules.Scheduling.Domain.Exceptions;
using ErpClink.Modules.Scheduling.Domain.Holidays;
using ErpClink.Modules.Scheduling.Domain.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpClink.Modules.Scheduling.Infrastructure.Persistence.Configurations;

public sealed class DoctorWorkingScheduleConfiguration : IEntityTypeConfiguration<DoctorWorkingSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorWorkingSchedule> builder)
    {
        builder.ToTable("DoctorWorkingSchedules");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.StartTime).HasColumnType("time");
        builder.Property(x => x.EndTime).HasColumnType("time");

        builder.HasIndex(x => new { x.OrganizationId, x.DoctorId, x.ClinicId, x.DayOfWeek, x.IsActive });
        builder.HasIndex(x => new { x.OrganizationId, x.DoctorId, x.IsActive });
        builder.HasIndex(x => x.BranchId);
        builder.HasIndex(x => new { x.ClinicId, x.IsActive });
    }
}

public sealed class ClinicHolidayConfiguration : IEntityTypeConfiguration<ClinicHoliday>
{
    public void Configure(EntityTypeBuilder<ClinicHoliday> builder)
    {
        builder.ToTable("ClinicHolidays");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.HasIndex(x => new { x.OrganizationId, x.Date, x.IsActive });
        builder.HasIndex(x => new { x.OrganizationId, x.Date })
            .IsUnique()
            .HasFilter("[IsActive] = 1 AND [BranchId] IS NULL");
    }
}

public sealed class DoctorScheduleExceptionConfiguration : IEntityTypeConfiguration<DoctorScheduleException>
{
    public void Configure(EntityTypeBuilder<DoctorScheduleException> builder)
    {
        builder.ToTable("DoctorScheduleExceptions");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.StartTime).HasColumnType("time");
        builder.Property(x => x.EndTime).HasColumnType("time");
        builder.HasIndex(x => new { x.OrganizationId, x.DoctorId, x.Date, x.IsActive });
        builder.HasIndex(x => x.BranchId);
    }
}
