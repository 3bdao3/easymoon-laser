using ErpClink.Modules.Prescriptions.Domain.Medications;
using ErpClink.Modules.Prescriptions.Domain.Prescriptions;
using ErpClink.Modules.Prescriptions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpClink.Modules.Prescriptions.Infrastructure.Persistence.Configurations;

public sealed class MedicationConfiguration : IEntityTypeConfiguration<Medication>
{
    public void Configure(EntityTypeBuilder<Medication> builder)
    {
        builder.ToTable("Medications");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.GenericName).HasMaxLength(200);
        builder.Property(x => x.Strength).HasMaxLength(64);
        builder.Property(x => x.DosageForm).HasMaxLength(64);
        builder.Property(x => x.Route).HasMaxLength(64);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);

        builder.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.Name });
        builder.HasIndex(x => new { x.OrganizationId, x.GenericName });
        builder.HasIndex(x => new { x.OrganizationId, x.IsActive });
    }
}

public sealed class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("Prescriptions");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.IsActive);

        builder.Property(x => x.PrescriptionNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.IssuedBy).HasMaxLength(64);
        builder.Property(x => x.CancelledBy).HasMaxLength(64);
        builder.Property(x => x.CancellationReason).HasMaxLength(1000);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => new { x.OrganizationId, x.PrescriptionNumber }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.MedicalVisitId })
            .IsUnique()
            .HasFilter("[Status] IN ('Draft', 'Issued')")
            .HasDatabaseName("IX_Prescriptions_ActiveMedicalVisit");

        builder.HasIndex(x => new { x.OrganizationId, x.PatientId, x.PrescriptionDate });
        builder.HasIndex(x => new { x.OrganizationId, x.DoctorId, x.PrescriptionDate });
        builder.HasIndex(x => new { x.OrganizationId, x.Status, x.PrescriptionDate });
        builder.HasIndex(x => x.BranchId);
    }
}

public sealed class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.ToTable("PrescriptionItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MedicationNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Dosage).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Frequency).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Duration).HasMaxLength(128);
        builder.Property(x => x.Route).HasMaxLength(64);
        builder.Property(x => x.Instructions).HasMaxLength(1000);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.Quantity).HasPrecision(18, 2);

        builder.HasIndex(x => x.PrescriptionId);
        builder.HasIndex(x => x.MedicationId);
    }
}

public sealed class PrescriptionNumberSequenceConfiguration : IEntityTypeConfiguration<PrescriptionNumberSequence>
{
    public void Configure(EntityTypeBuilder<PrescriptionNumberSequence> builder)
    {
        builder.ToTable("PrescriptionNumberSequences");
        builder.HasKey(x => new { x.OrganizationId, x.Year });
    }
}
