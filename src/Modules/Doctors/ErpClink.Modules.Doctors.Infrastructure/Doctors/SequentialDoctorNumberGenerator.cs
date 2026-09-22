using ErpClink.Modules.Doctors.Application.Doctors;
using ErpClink.Modules.Doctors.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Doctors.Infrastructure.Doctors;

/// <summary>Temporary format: D-{yyyy}-{000001} per organization. Replace via IDoctorNumberGenerator.</summary>
public sealed class SequentialDoctorNumberGenerator : IDoctorNumberGenerator
{
    private readonly DoctorsDbContext _db;

    public SequentialDoctorNumberGenerator(DoctorsDbContext db) => _db = db;

    public async Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var sequence = await _db.DoctorNumberSequences
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Year == year, cancellationToken);

        if (sequence is null)
        {
            sequence = new DoctorNumberSequence { OrganizationId = organizationId, Year = year, LastValue = 0 };
            _db.DoctorNumberSequences.Add(sequence);
        }

        sequence.LastValue += 1;
        await _db.SaveChangesAsync(cancellationToken);
        return $"D-{year}-{sequence.LastValue:D6}";
    }
}
