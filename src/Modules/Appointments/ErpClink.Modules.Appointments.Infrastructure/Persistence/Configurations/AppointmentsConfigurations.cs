using ErpClink.Modules.Appointments.Domain.Appointments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpClink.Modules.Appointments.Infrastructure.Persistence.Configurations;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.OccupiesSlot);
        // History collections are domain-side; persisted via dedicated tables/configs without aggregate cascade.
        builder.Ignore(x => x.StatusHistory);
        builder.Ignore(x => x.RescheduleHistory);

        builder.Property(x => x.AppointmentNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.CancelledBy).HasMaxLength(64);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.StartTime).HasColumnType("time");
        builder.Property(x => x.EndTime).HasColumnType("time");

        builder.HasIndex(x => new { x.OrganizationId, x.AppointmentNumber }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.PatientId, x.AppointmentDate });
        builder.HasIndex(x => new { x.OrganizationId, x.DoctorId, x.AppointmentDate });
        builder.HasIndex(x => new { x.OrganizationId, x.ClinicId, x.AppointmentDate });
        builder.HasIndex(x => new { x.OrganizationId, x.Status, x.AppointmentDate });
        builder.HasIndex(x => x.BranchId);

        // Fixed-slot occupancy: one active booking per doctor/clinic/date/start.
        builder.HasIndex(x => new { x.OrganizationId, x.DoctorId, x.ClinicId, x.AppointmentDate, x.StartTime })
            .IsUnique()
            .HasFilter("[Status] IN ('Scheduled', 'Confirmed', 'CheckedIn')")
            .HasDatabaseName("IX_Appointments_ActiveSlot");
    }
}

public sealed class AppointmentStatusHistoryConfiguration : IEntityTypeConfiguration<AppointmentStatusHistoryEntry>
{
    public void Configure(EntityTypeBuilder<AppointmentStatusHistoryEntry> builder)
    {
        builder.ToTable("AppointmentStatusHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ChangedBy).HasMaxLength(64);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasIndex(x => x.AppointmentId);
        builder.HasOne<Appointment>().WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AppointmentRescheduleHistoryConfiguration : IEntityTypeConfiguration<AppointmentRescheduleHistoryEntry>
{
    public void Configure(EntityTypeBuilder<AppointmentRescheduleHistoryEntry> builder)
    {
        builder.ToTable("AppointmentRescheduleHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ChangedBy).HasMaxLength(64);
        builder.Property(x => x.PreviousStart).HasColumnType("time");
        builder.Property(x => x.PreviousEnd).HasColumnType("time");
        builder.Property(x => x.NewStart).HasColumnType("time");
        builder.Property(x => x.NewEnd).HasColumnType("time");
        builder.HasIndex(x => x.AppointmentId);
        builder.HasOne<Appointment>().WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AppointmentNumberSequenceConfiguration : IEntityTypeConfiguration<AppointmentNumberSequence>
{
    public void Configure(EntityTypeBuilder<AppointmentNumberSequence> builder)
    {
        builder.ToTable("AppointmentNumberSequences");
        builder.HasKey(x => new { x.OrganizationId, x.Year });
    }
}
