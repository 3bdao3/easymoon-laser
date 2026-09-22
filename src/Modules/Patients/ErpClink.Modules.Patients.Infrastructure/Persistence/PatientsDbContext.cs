using ErpClink.Modules.Patients.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Patients.Infrastructure.Persistence;

public sealed class PatientsDbContext : DbContext
{
    public PatientsDbContext(DbContextOptions<PatientsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientAllergy> PatientAllergies => Set<PatientAllergy>();
    public DbSet<PatientMedicalHistoryItem> PatientMedicalHistoryItems => Set<PatientMedicalHistoryItem>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();
    public DbSet<PatientNumberSequence> PatientNumberSequences => Set<PatientNumberSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("patients");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PatientsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
