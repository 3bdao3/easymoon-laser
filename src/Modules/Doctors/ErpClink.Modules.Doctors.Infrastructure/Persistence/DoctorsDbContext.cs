using ErpClink.Modules.Doctors.Domain.Clinics;
using ErpClink.Modules.Doctors.Domain.Doctors;
using ErpClink.Modules.Doctors.Domain.Nurses;
using ErpClink.Modules.Doctors.Domain.Specialties;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Doctors.Infrastructure.Persistence;

public sealed class DoctorNumberSequence
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public long LastValue { get; set; }
}

public sealed class DoctorsDbContext : DbContext
{
    public DoctorsDbContext(DbContextOptions<DoctorsDbContext> options) : base(options)
    {
    }

    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Nurse> Nurses => Set<Nurse>();
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<DoctorClinicAssignment> DoctorClinicAssignments => Set<DoctorClinicAssignment>();
    public DbSet<DoctorNumberSequence> DoctorNumberSequences => Set<DoctorNumberSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("doctors");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DoctorsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
