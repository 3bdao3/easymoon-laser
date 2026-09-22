using ErpClink.Modules.Prescriptions.Application.Prescriptions;
using ErpClink.Modules.Prescriptions.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Prescriptions.Infrastructure.Prescriptions;

/// <summary>Format RX-{yyyy}-{000001} per organization.</summary>
public sealed class SequentialPrescriptionNumberGenerator : IPrescriptionNumberGenerator
{
    private readonly PrescriptionsDbContext _db;

    public SequentialPrescriptionNumberGenerator(PrescriptionsDbContext db) => _db = db;

    public async Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                await _db.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (
    SELECT 1 FROM [prescriptions].[PrescriptionNumberSequences] WITH (UPDLOCK, HOLDLOCK)
    WHERE [OrganizationId] = {organizationId} AND [Year] = {year})
BEGIN
    INSERT INTO [prescriptions].[PrescriptionNumberSequences] ([OrganizationId], [Year], [LastValue])
    VALUES ({organizationId}, {year}, 0);
END", cancellationToken);
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                // Concurrent first insert — retry.
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
            }

            var next = await _db.Database
                .SqlQuery<long>($@"
UPDATE [prescriptions].[PrescriptionNumberSequences] WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
SET [LastValue] = [LastValue] + 1
OUTPUT INSERTED.[LastValue]
WHERE [OrganizationId] = {organizationId} AND [Year] = {year}")
                .ToListAsync(cancellationToken);

            if (next.Count == 1)
                return $"RX-{year}-{next[0]:D6}";
        }

        throw new InvalidOperationException("Failed to allocate a prescription number under concurrency.");
    }
}
