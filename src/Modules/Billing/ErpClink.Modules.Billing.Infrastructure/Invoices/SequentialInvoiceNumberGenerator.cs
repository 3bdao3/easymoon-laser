using ErpClink.Modules.Billing.Application.Invoices;
using ErpClink.Modules.Billing.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Billing.Infrastructure.Invoices;

/// <summary>Format INV-{yyyy}-{000001} per organization.</summary>
public sealed class SequentialInvoiceNumberGenerator : IInvoiceNumberGenerator
{
    private readonly BillingDbContext _db;

    public SequentialInvoiceNumberGenerator(BillingDbContext db) => _db = db;

    public async Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                await _db.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (
    SELECT 1 FROM [billing].[InvoiceNumberSequences] WITH (UPDLOCK, HOLDLOCK)
    WHERE [OrganizationId] = {organizationId} AND [Year] = {year})
BEGIN
    INSERT INTO [billing].[InvoiceNumberSequences] ([OrganizationId], [Year], [LastValue])
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
UPDATE [billing].[InvoiceNumberSequences] WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
SET [LastValue] = [LastValue] + 1
OUTPUT INSERTED.[LastValue]
WHERE [OrganizationId] = {organizationId} AND [Year] = {year}")
                .ToListAsync(cancellationToken);

            if (next.Count == 1)
                return $"INV-{year}-{next[0]:D6}";
        }

        throw new InvalidOperationException("Failed to allocate an invoice number under concurrency.");
    }
}
