using ErpClink.Modules.MedicalVisits.Domain.Visits;
using ErpClink.Modules.MedicalVisits.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpClink.Modules.MedicalVisits.Infrastructure.Persistence.Configurations;

public sealed class MedicalVisitConfiguration : IEntityTypeConfiguration<MedicalVisit>
{
    public void Configure(EntityTypeBuilder<MedicalVisit> builder)
    {
        builder.ToTable("MedicalVisits");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.IsActive);

        builder.Property(x => x.VisitNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ChiefComplaint).HasMaxLength(1000);
        builder.Property(x => x.ClinicalNotes).HasMaxLength(4000);
        builder.Property(x => x.ExaminationFindings).HasMaxLength(4000);
        builder.Property(x => x.DiagnosisNotes).HasMaxLength(2000);
        builder.Property(x => x.FollowUpNotes).HasMaxLength(2000);
        builder.Property(x => x.CompletedBy).HasMaxLength(64);
        builder.Property(x => x.CancelledBy).HasMaxLength(64);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.OrganizationId, x.VisitNumber }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.AppointmentId })
            .IsUnique()
            .HasFilter("[Status] IN ('Open', 'InProgress')")
            .HasDatabaseName("IX_MedicalVisits_ActiveAppointment");

        builder.HasIndex(x => new { x.OrganizationId, x.PatientId, x.VisitDate });
        builder.HasIndex(x => new { x.OrganizationId, x.DoctorId, x.VisitDate });
        builder.HasIndex(x => new { x.OrganizationId, x.ClinicId, x.VisitDate });
        builder.HasIndex(x => new { x.OrganizationId, x.Status, x.VisitDate });
        builder.HasIndex(x => x.BranchId);
        builder.HasIndex(x => x.QueueEntryId);
    }
}

public sealed class VisitNumberSequenceConfiguration : IEntityTypeConfiguration<VisitNumberSequence>
{
    public void Configure(EntityTypeBuilder<VisitNumberSequence> builder)
    {
        builder.ToTable("VisitNumberSequences");
        builder.HasKey(x => new { x.OrganizationId, x.Year });
    }
}
