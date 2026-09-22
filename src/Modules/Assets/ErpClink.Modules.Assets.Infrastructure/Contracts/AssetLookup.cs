using ErpClink.Modules.Assets.Application.Contracts;
using ErpClink.Modules.Assets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Assets.Infrastructure.Contracts;

public sealed class AssetLookup : IAssetLookup
{
    private readonly AssetsDbContext _db;

    public AssetLookup(AssetsDbContext db) => _db = db;

    public Task<bool> ExistsInOrganizationAsync(Guid assetId, Guid organizationId, CancellationToken cancellationToken = default) =>
        _db.Assets.AsNoTracking().AnyAsync(a => a.Id == assetId && a.OrganizationId == organizationId, cancellationToken);
}
