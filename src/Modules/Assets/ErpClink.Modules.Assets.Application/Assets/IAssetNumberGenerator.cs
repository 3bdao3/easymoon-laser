namespace ErpClink.Modules.Assets.Application.Assets;

public interface IAssetNumberGenerator
{
    Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
