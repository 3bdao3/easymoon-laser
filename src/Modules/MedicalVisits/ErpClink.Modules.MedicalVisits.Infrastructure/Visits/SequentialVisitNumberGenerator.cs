using ErpClink.Modules.MedicalVisits.Application.Visits;
using ErpClink.Modules.MedicalVisits.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.MedicalVisits.Infrastructure.Visits;

/// <summary>Format V-{yyyy}-{000001} per organization. Uses UPDLOCK for concurrency safety.</summary>
public sealed class SequentialVisitNumberGenerator : IVisitNumberGenerator
{
    private readonly MedicalVisitsDbContext _db;

    public SequentialVisitNumberGenerator(MedicalVisitsDbContext db) => _db = db;

    public async Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;

        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                await _db.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (
    SELECT 1
    FROM [medical_visits].[VisitNumberSequences] WITH (UPDLOCK, HOLDLOCK)
    WHERE [OrganizationId] = {organizationId} AND [Year] = {year})
BEGIN
    INSERT INTO [medical_visits].[VisitNumberSequences] ([OrganizationId], [Year], [LastValue])
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

            var nextValues = await _db.Database
                .SqlQuery<long>($@"
UPDATE [medical_visits].[VisitNumberSequences] WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
SET [LastValue] = [LastValue] + 1
OUTPUT INSERTED.[LastValue]
WHERE [OrganizationId] = {organizationId} AND [Year] = {year}")
                .ToListAsync(cancellationToken);

            if (nextValues.Count == 1)
                return $"V-{year}-{nextValues[0]:D6}";
        }

        throw new InvalidOperationException("Failed to allocate a visit number under concurrency.");
    }
}
