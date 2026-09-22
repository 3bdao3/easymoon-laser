namespace ErpClink.Modules.Assets.Application.Locations;

public interface IAssetLocationCodeGenerator
{
    Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
