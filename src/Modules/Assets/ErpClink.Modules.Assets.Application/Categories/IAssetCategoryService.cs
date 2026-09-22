using ErpClink.Modules.Assets.Application.Categories.Models;

namespace ErpClink.Modules.Assets.Application.Categories;

public interface IAssetCategoryService
{
    Task<AssetCategoryDto> CreateAsync(CreateAssetCategoryRequest request, CancellationToken cancellationToken = default);
    Task<AssetCategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedAssetCategoriesResult> SearchAsync(SearchAssetCategoriesRequest request, CancellationToken cancellationToken = default);
    Task<AssetCategoryDto> UpdateAsync(Guid id, UpdateAssetCategoryRequest request, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
