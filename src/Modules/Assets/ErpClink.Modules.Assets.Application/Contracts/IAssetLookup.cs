namespace ErpClink.Modules.Assets.Application.Contracts;

public interface IAssetLookup
{
    Task<bool> ExistsInOrganizationAsync(Guid assetId, Guid organizationId, CancellationToken cancellationToken = default);
}
