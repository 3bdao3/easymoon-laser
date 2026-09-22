using ErpClink.Modules.Inventory.Application.GoodsReceipts;
using ErpClink.Modules.Inventory.Application.Items;
using ErpClink.Modules.Inventory.Application.Warehouses;
using ErpClink.Modules.Inventory.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Inventory.Infrastructure.Numbering;

public sealed class SequentialWarehouseNumberGenerator : IWarehouseNumberGenerator
{
    private readonly InventoryDbContext _db;
    public SequentialWarehouseNumberGenerator(InventoryDbContext db) => _db = db;

    public async Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var next = await AllocateAsync(_db, organizationId, year, "WarehouseNumberSequences", cancellationToken);
        return $"WH-{year}-{next:D6}";
    }

    internal static async Task<long> AllocateAsync(
        InventoryDbContext db,
        Guid organizationId,
        int year,
        string table,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                if (table == "WarehouseNumberSequences")
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (SELECT 1 FROM [inventory].[WarehouseNumberSequences] WITH (UPDLOCK, HOLDLOCK) WHERE [OrganizationId] = {organizationId} AND [Year] = {year})
BEGIN INSERT INTO [inventory].[WarehouseNumberSequences] ([OrganizationId], [Year], [LastValue]) VALUES ({organizationId}, {year}, 0); END", cancellationToken);
                }
                else if (table == "InventoryItemNumberSequences")
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (SELECT 1 FROM [inventory].[InventoryItemNumberSequences] WITH (UPDLOCK, HOLDLOCK) WHERE [OrganizationId] = {organizationId} AND [Year] = {year})
BEGIN INSERT INTO [inventory].[InventoryItemNumberSequences] ([OrganizationId], [Year], [LastValue]) VALUES ({organizationId}, {year}, 0); END", cancellationToken);
                }
                else
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (SELECT 1 FROM [inventory].[GoodsReceiptNumberSequences] WITH (UPDLOCK, HOLDLOCK) WHERE [OrganizationId] = {organizationId} AND [Year] = {year})
BEGIN INSERT INTO [inventory].[GoodsReceiptNumberSequences] ([OrganizationId], [Year], [LastValue]) VALUES ({organizationId}, {year}, 0); END", cancellationToken);
                }
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627) { }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 }) { }

            FormattableString update = table switch
            {
                "WarehouseNumberSequences" => $@"
UPDATE [inventory].[WarehouseNumberSequences] WITH (UPDLOCK, ROWLOCK, HOLDLOCK) SET [LastValue] = [LastValue] + 1 OUTPUT INSERTED.[LastValue]
WHERE [OrganizationId] = {organizationId} AND [Year] = {year}",
                "InventoryItemNumberSequences" => $@"
UPDATE [inventory].[InventoryItemNumberSequences] WITH (UPDLOCK, ROWLOCK, HOLDLOCK) SET [LastValue] = [LastValue] + 1 OUTPUT INSERTED.[LastValue]
WHERE [OrganizationId] = {organizationId} AND [Year] = {year}",
                _ => $@"
UPDATE [inventory].[GoodsReceiptNumberSequences] WITH (UPDLOCK, ROWLOCK, HOLDLOCK) SET [LastValue] = [LastValue] + 1 OUTPUT INSERTED.[LastValue]
WHERE [OrganizationId] = {organizationId} AND [Year] = {year}"
            };

            var next = await db.Database.SqlQuery<long>(update).ToListAsync(cancellationToken);
            if (next.Count == 1)
                return next[0];
        }

        throw new InvalidOperationException($"Failed to allocate inventory sequence for {table}.");
    }
}

public sealed class SequentialInventoryItemNumberGenerator : IInventoryItemNumberGenerator
{
    private readonly InventoryDbContext _db;
    public SequentialInventoryItemNumberGenerator(InventoryDbContext db) => _db = db;

    public async Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var next = await SequentialWarehouseNumberGenerator.AllocateAsync(_db, organizationId, year, "InventoryItemNumberSequences", cancellationToken);
        return $"ITM-{year}-{next:D6}";
    }
}

public sealed class SequentialGoodsReceiptNumberGenerator : IGoodsReceiptNumberGenerator
{
    private readonly InventoryDbContext _db;
    public SequentialGoodsReceiptNumberGenerator(InventoryDbContext db) => _db = db;

    public async Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var next = await SequentialWarehouseNumberGenerator.AllocateAsync(_db, organizationId, year, "GoodsReceiptNumberSequences", cancellationToken);
        return $"GR-{year}-{next:D6}";
    }
}
