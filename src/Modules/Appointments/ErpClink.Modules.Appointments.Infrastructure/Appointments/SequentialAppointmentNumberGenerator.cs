using ErpClink.Modules.Appointments.Application.Appointments;
using ErpClink.Modules.Appointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Appointments.Infrastructure.Appointments;

/// <summary>Temporary format: A-{yyyy}-{000001} per organization. Replace via IAppointmentNumberGenerator.</summary>
public sealed class SequentialAppointmentNumberGenerator : IAppointmentNumberGenerator
{
    private readonly AppointmentsDbContext _db;

    public SequentialAppointmentNumberGenerator(AppointmentsDbContext db) => _db = db;

    public async Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var sequence = await _db.AppointmentNumberSequences
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Year == year, cancellationToken);

        if (sequence is null)
        {
            sequence = new AppointmentNumberSequence { OrganizationId = organizationId, Year = year, LastValue = 0 };
            _db.AppointmentNumberSequences.Add(sequence);
        }

        sequence.LastValue += 1;
        // Persist with the booking SaveChanges (caller owns transaction).
        return $"A-{year}-{sequence.LastValue:D6}";
    }
}
