using ErpClink.Modules.Queue.Application.Entries;
using ErpClink.Modules.Queue.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Queue.Infrastructure.Entries;

/// <summary>
/// Scope: Organization + Branch + Clinic + Date. Format Q-{yyyyMMdd}-{0001}.
/// Uses UPDLOCK/HOLDLOCK so concurrent check-ins never share the same number.
/// </summary>
public sealed class SequentialQueueNumberGenerator : IQueueNumberGenerator
{
    private readonly QueueDbContext _db;

    public SequentialQueueNumberGenerator(QueueDbContext db) => _db = db;

    public async Task<string> GenerateAsync(
        Guid organizationId,
        Guid branchId,
        Guid clinicId,
        DateOnly queueDate,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                await _db.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (
    SELECT 1
    FROM [queue].[QueueNumberSequences] WITH (UPDLOCK, HOLDLOCK)
    WHERE [OrganizationId] = {organizationId}
      AND [BranchId] = {branchId}
      AND [ClinicId] = {clinicId}
      AND [QueueDate] = {queueDate})
BEGIN
    INSERT INTO [queue].[QueueNumberSequences]
        ([OrganizationId], [BranchId], [ClinicId], [QueueDate], [LastValue])
    VALUES ({organizationId}, {branchId}, {clinicId}, {queueDate}, 0);
END", cancellationToken);
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
            }

            var nextValues = await _db.Database
                .SqlQuery<long>($@"
UPDATE [queue].[QueueNumberSequences] WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
SET [LastValue] = [LastValue] + 1
OUTPUT INSERTED.[LastValue]
WHERE [OrganizationId] = {organizationId}
  AND [BranchId] = {branchId}
  AND [ClinicId] = {clinicId}
  AND [QueueDate] = {queueDate}")
                .ToListAsync(cancellationToken);

            if (nextValues.Count == 1)
                return $"Q-{queueDate:yyyyMMdd}-{nextValues[0]:D4}";
        }

        throw new InvalidOperationException("Failed to allocate a queue number under concurrency.");
    }
}
