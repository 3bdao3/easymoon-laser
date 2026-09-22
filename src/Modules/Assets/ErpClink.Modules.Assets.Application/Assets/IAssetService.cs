using ErpClink.Modules.Assets.Application.Assets.Models;

namespace ErpClink.Modules.Assets.Application.Assets;

public interface IAssetService
{
    Task<AssetDto> CreateAsync(CreateAssetRequest request, CancellationToken cancellationToken = default);
    Task<AssetDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedAssetsResult> SearchAsync(SearchAssetsRequest request, CancellationToken cancellationToken = default);
    Task<AssetDto> UpdateAsync(Guid id, UpdateAssetRequest request, CancellationToken cancellationToken = default);
    Task<AssetDto> ChangeLocationAsync(Guid id, ChangeAssetLocationRequest request, CancellationToken cancellationToken = default);
    Task<AssetDto> StartMaintenanceAsync(Guid id, StartAssetMaintenanceRequest request, CancellationToken cancellationToken = default);
    Task<AssetDto> CompleteMaintenanceAsync(Guid id, CompleteAssetMaintenanceRequest request, CancellationToken cancellationToken = default);
    Task<AssetDto> RetireAsync(Guid id, RetireAssetRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssetHistoryEntryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default);
}
