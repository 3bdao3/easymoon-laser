using ErpClink.Modules.Patients.Application.Patients;
using ErpClink.Modules.Patients.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Patients.Infrastructure.Patients;

/// <summary>
/// Temporary configurable format: P-{yyyy}-{sequence:D6} per organization.
/// Replace via IPatientNumberGenerator without changing Patient aggregate.
/// </summary>
public sealed class SequentialPatientNumberGenerator : IPatientNumberGenerator
{
    private readonly PatientsDbContext _dbContext;

    public SequentialPatientNumberGenerator(PatientsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;

        var sequence = await _dbContext.PatientNumberSequences
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Year == year, cancellationToken);

        if (sequence is null)
        {
            sequence = new PatientNumberSequence
            {
                OrganizationId = organizationId,
                Year = year,
                LastValue = 0
            };
            _dbContext.PatientNumberSequences.Add(sequence);
        }

        sequence.LastValue += 1;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return $"P-{year}-{sequence.LastValue:D6}";
    }
}
