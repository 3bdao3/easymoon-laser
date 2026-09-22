using ErpClink.Modules.Assets.Application.Assets;
using ErpClink.Modules.Assets.Application.Categories;
using ErpClink.Modules.Assets.Application.Locations;
using ErpClink.Modules.Assets.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Assets.Infrastructure.Numbering;

public sealed class SequentialAssetCategoryCodeGenerator : IAssetCategoryCodeGenerator
{
    private readonly AssetsDbContext _db;
    public SequentialAssetCategoryCodeGenerator(AssetsDbContext db) => _db = db;

    public async Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var next = await AssetsSequenceAllocator.AllocateAsync(_db, organizationId, year, "AssetCategoryNumberSequences", cancellationToken);
        return $"ASC-{year}-{next:D6}";
    }
}

public sealed class SequentialAssetLocationCodeGenerator : IAssetLocationCodeGenerator
{
    private readonly AssetsDbContext _db;
    public SequentialAssetLocationCodeGenerator(AssetsDbContext db) => _db = db;

    public async Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var next = await AssetsSequenceAllocator.AllocateAsync(_db, organizationId, year, "AssetLocationNumberSequences", cancellationToken);
        return $"LOC-{year}-{next:D6}";
    }
}

public sealed class SequentialAssetNumberGenerator : IAssetNumberGenerator
{
    private readonly AssetsDbContext _db;
    public SequentialAssetNumberGenerator(AssetsDbContext db) => _db = db;

    public async Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var next = await AssetsSequenceAllocator.AllocateAsync(_db, organizationId, year, "AssetNumberSequences", cancellationToken);
        return $"AST-{year}-{next:D6}";
    }
}

internal static class AssetsSequenceAllocator
{
    internal static async Task<long> AllocateAsync(
        AssetsDbContext db,
        Guid organizationId,
        int year,
        string table,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                if (table == "AssetCategoryNumberSequences")
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (SELECT 1 FROM [assets].[AssetCategoryNumberSequences] WITH (UPDLOCK, HOLDLOCK) WHERE [OrganizationId] = {organizationId} AND [Year] = {year})
BEGIN INSERT INTO [assets].[AssetCategoryNumberSequences] ([OrganizationId], [Year], [LastValue]) VALUES ({organizationId}, {year}, 0); END", cancellationToken);
                }
                else if (table == "AssetLocationNumberSequences")
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (SELECT 1 FROM [assets].[AssetLocationNumberSequences] WITH (UPDLOCK, HOLDLOCK) WHERE [OrganizationId] = {organizationId} AND [Year] = {year})
BEGIN INSERT INTO [assets].[AssetLocationNumberSequences] ([OrganizationId], [Year], [LastValue]) VALUES ({organizationId}, {year}, 0); END", cancellationToken);
                }
                else
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (SELECT 1 FROM [assets].[AssetNumberSequences] WITH (UPDLOCK, HOLDLOCK) WHERE [OrganizationId] = {organizationId} AND [Year] = {year})
BEGIN INSERT INTO [assets].[AssetNumberSequences] ([OrganizationId], [Year], [LastValue]) VALUES ({organizationId}, {year}, 0); END", cancellationToken);
                }
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627) { }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 }) { }

            FormattableString update = table switch
            {
                "AssetCategoryNumberSequences" => $@"
UPDATE [assets].[AssetCategoryNumberSequences] WITH (UPDLOCK, ROWLOCK, HOLDLOCK) SET [LastValue] = [LastValue] + 1 OUTPUT INSERTED.[LastValue]
WHERE [OrganizationId] = {organizationId} AND [Year] = {year}",
                "AssetLocationNumberSequences" => $@"
UPDATE [assets].[AssetLocationNumberSequences] WITH (UPDLOCK, ROWLOCK, HOLDLOCK) SET [LastValue] = [LastValue] + 1 OUTPUT INSERTED.[LastValue]
WHERE [OrganizationId] = {organizationId} AND [Year] = {year}",
                _ => $@"
UPDATE [assets].[AssetNumberSequences] WITH (UPDLOCK, ROWLOCK, HOLDLOCK) SET [LastValue] = [LastValue] + 1 OUTPUT INSERTED.[LastValue]
WHERE [OrganizationId] = {organizationId} AND [Year] = {year}"
            };

            var next = await db.Database.SqlQuery<long>(update).ToListAsync(cancellationToken);
            if (next.Count == 1)
                return next[0];
        }

        throw new InvalidOperationException($"Failed to allocate assets sequence for {table}.");
    }
}
