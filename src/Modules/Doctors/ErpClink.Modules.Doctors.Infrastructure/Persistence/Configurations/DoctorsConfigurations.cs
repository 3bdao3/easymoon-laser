using ErpClink.Modules.Doctors.Domain.Clinics;
using ErpClink.Modules.Doctors.Domain.Doctors;
using ErpClink.Modules.Doctors.Domain.Nurses;
using ErpClink.Modules.Doctors.Domain.Specialties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErpClink.Modules.Doctors.Infrastructure.Persistence.Configurations;

public sealed class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.DoctorNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(450);
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LicenseNumber).HasMaxLength(50);
        builder.Property(x => x.PhoneNumber).HasMaxLength(30);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);

        builder.HasIndex(x => new { x.OrganizationId, x.DoctorNumber }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.UserId }).HasFilter("[UserId] IS NOT NULL");
        builder.HasIndex(x => new { x.OrganizationId, x.SpecialtyId });
        builder.HasIndex(x => new { x.OrganizationId, x.IsActive });
        builder.HasIndex(x => x.BranchId);
        builder.HasIndex(x => new { x.OrganizationId, x.LastName, x.FirstName });
    }
}

public sealed class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.ToTable("Clinics");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Location).HasMaxLength(200);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);

        builder.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.IsActive });
        builder.HasIndex(x => x.BranchId);
    }
}

public sealed class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.ToTable("Specialties");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.IsActive });
    }
}

public sealed class DoctorClinicAssignmentConfiguration : IEntityTypeConfiguration<DoctorClinicAssignment>
{
    public void Configure(EntityTypeBuilder<DoctorClinicAssignment> builder)
    {
        builder.ToTable("DoctorClinicAssignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);

        builder.HasIndex(x => new { x.OrganizationId, x.DoctorId, x.ClinicId, x.IsActive });
        builder.HasIndex(x => new { x.DoctorId, x.ClinicId })
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.HasOne<Doctor>().WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Clinic>().WithMany().HasForeignKey(x => x.ClinicId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class NurseConfiguration : IEntityTypeConfiguration<Nurse>
{
    public void Configure(EntityTypeBuilder<Nurse> builder)
    {
        builder.ToTable("Nurses");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Property(x => x.NurseNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(450);
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(30);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.UpdatedBy).HasMaxLength(64);
        builder.HasIndex(x => new { x.OrganizationId, x.NurseNumber }).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.IsActive });
    }
}

public sealed class DoctorNumberSequenceConfiguration : IEntityTypeConfiguration<DoctorNumberSequence>
{
    public void Configure(EntityTypeBuilder<DoctorNumberSequence> builder)
    {
        builder.ToTable("DoctorNumberSequences");
        builder.HasKey(x => new { x.OrganizationId, x.Year });
    }
}
