using ErpClink.Modules.Patients.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpClink.Modules.Patients.Infrastructure.Persistence.Configurations;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PatientNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.MiddleName).HasMaxLength(100);
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NationalId).HasMaxLength(50);
        builder.Property(x => x.PhoneNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.AddressLine1).HasMaxLength(200);
        builder.Property(x => x.AddressLine2).HasMaxLength(200);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.EmergencyContactName).HasMaxLength(150);
        builder.Property(x => x.EmergencyContactPhone).HasMaxLength(30);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.Gender).HasConversion<int>();

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.FullName);

        builder.HasIndex(x => new { x.OrganizationId, x.PatientNumber }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.NationalId })
            .IsUnique()
            .HasFilter("[NationalId] IS NOT NULL");
        builder.HasIndex(x => new { x.OrganizationId, x.PhoneNumber });
        builder.HasIndex(x => new { x.OrganizationId, x.LastName, x.FirstName });
        builder.HasIndex(x => new { x.OrganizationId, x.IsActive });
        builder.HasIndex(x => x.BranchId);

        builder.HasMany(x => x.Allergies)
            .WithOne()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.MedicalHistory)
            .WithOne()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Allergies).HasField("_allergies").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.MedicalHistory).HasField("_medicalHistory").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class PatientAllergyConfiguration : IEntityTypeConfiguration<PatientAllergy>
{
    public void Configure(EntityTypeBuilder<PatientAllergy> builder)
    {
        builder.ToTable("PatientAllergies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Reaction).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.Severity).HasConversion<int>();
        builder.HasIndex(x => new { x.PatientId, x.IsActive });
    }
}

public sealed class PatientMedicalHistoryItemConfiguration : IEntityTypeConfiguration<PatientMedicalHistoryItem>
{
    public void Configure(EntityTypeBuilder<PatientMedicalHistoryItem> builder)
    {
        builder.ToTable("PatientMedicalHistoryItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.HasIndex(x => new { x.PatientId, x.IsActive });
    }
}

public sealed class PatientNumberSequenceConfiguration : IEntityTypeConfiguration<PatientNumberSequence>
{
    public void Configure(EntityTypeBuilder<PatientNumberSequence> builder)
    {
        builder.ToTable("PatientNumberSequences");
        builder.HasKey(x => new { x.OrganizationId, x.Year });
        builder.Property(x => x.LastValue).IsRequired();
    }
}

public sealed class PatientDocumentConfiguration : IEntityTypeConfiguration<PatientDocument>
{
    public void Configure(EntityTypeBuilder<PatientDocument> builder)
    {
        builder.ToTable("PatientDocuments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.FileExtension).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.Property(x => x.Category).HasConversion<int>();
        builder.Property(x => x.DocumentType).HasConversion<int>();

        builder.HasIndex(x => x.StorageKey).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.PatientId, x.IsActive });
        builder.HasIndex(x => new { x.PatientId, x.Category, x.IsActive });
        builder.HasIndex(x => new { x.PatientId, x.DocumentDate });
        builder.HasIndex(x => x.BranchId);

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
