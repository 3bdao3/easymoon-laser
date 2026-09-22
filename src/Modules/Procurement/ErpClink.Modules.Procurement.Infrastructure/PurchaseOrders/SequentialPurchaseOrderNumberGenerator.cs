using ErpClink.Modules.Procurement.Application.PurchaseOrders;
using ErpClink.Modules.Procurement.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Procurement.Infrastructure.PurchaseOrders;

/// <summary>Format PO-{yyyy}-{000001} per organization.</summary>
public sealed class SequentialPurchaseOrderNumberGenerator : IPurchaseOrderNumberGenerator
{
    private readonly ProcurementDbContext _db;

    public SequentialPurchaseOrderNumberGenerator(ProcurementDbContext db) => _db = db;

    public async Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                await _db.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (
    SELECT 1 FROM [procurement].[PurchaseOrderNumberSequences] WITH (UPDLOCK, HOLDLOCK)
    WHERE [OrganizationId] = {organizationId} AND [Year] = {year})
BEGIN
    INSERT INTO [procurement].[PurchaseOrderNumberSequences] ([OrganizationId], [Year], [LastValue])
    VALUES ({organizationId}, {year}, 0);
END", cancellationToken);
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
            }

            var next = await _db.Database
                .SqlQuery<long>($@"
UPDATE [procurement].[PurchaseOrderNumberSequences] WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
SET [LastValue] = [LastValue] + 1
OUTPUT INSERTED.[LastValue]
WHERE [OrganizationId] = {organizationId} AND [Year] = {year}")
                .ToListAsync(cancellationToken);

            if (next.Count == 1)
                return $"PO-{year}-{next[0]:D6}";
        }

        throw new InvalidOperationException("Failed to allocate a purchase order number under concurrency.");
    }
}
