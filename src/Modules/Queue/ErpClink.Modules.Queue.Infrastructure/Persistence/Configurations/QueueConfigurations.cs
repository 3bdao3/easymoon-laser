using ErpClink.Modules.Queue.Domain.Entries;
using ErpClink.Modules.Queue.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpClink.Modules.Queue.Infrastructure.Persistence.Configurations;

public sealed class QueueEntryConfiguration : IEntityTypeConfiguration<QueueEntry>
{
    public void Configure(EntityTypeBuilder<QueueEntry> builder)
    {
        builder.ToTable("QueueEntries");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.IsActive);

        builder.Property(x => x.QueueNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.OrganizationId, x.BranchId, x.QueueDate, x.ClinicId, x.QueueNumber }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.AppointmentId })
            .IsUnique()
            .HasFilter("[Status] IN ('Waiting', 'Called', 'InService')")
            .HasDatabaseName("IX_QueueEntries_ActiveAppointment");

        builder.HasIndex(x => new { x.OrganizationId, x.BranchId, x.QueueDate, x.Status });
        builder.HasIndex(x => new { x.OrganizationId, x.ClinicId, x.QueueDate });
        builder.HasIndex(x => new { x.OrganizationId, x.DoctorId, x.QueueDate });
        builder.HasIndex(x => new { x.OrganizationId, x.PatientId, x.QueueDate });
        builder.HasIndex(x => x.BranchId);
    }
}

public sealed class QueueNumberSequenceConfiguration : IEntityTypeConfiguration<QueueNumberSequence>
{
    public void Configure(EntityTypeBuilder<QueueNumberSequence> builder)
    {
        builder.ToTable("QueueNumberSequences");
        builder.HasKey(x => new { x.OrganizationId, x.BranchId, x.ClinicId, x.QueueDate });
    }
}
