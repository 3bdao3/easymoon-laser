using ErpClink.Modules.Assets.Application.Locations.Models;

namespace ErpClink.Modules.Assets.Application.Locations;

public interface IAssetLocationService
{
    Task<AssetLocationDto> CreateAsync(CreateAssetLocationRequest request, CancellationToken cancellationToken = default);
    Task<AssetLocationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedAssetLocationsResult> SearchAsync(SearchAssetLocationsRequest request, CancellationToken cancellationToken = default);
    Task<AssetLocationDto> UpdateAsync(Guid id, UpdateAssetLocationRequest request, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
