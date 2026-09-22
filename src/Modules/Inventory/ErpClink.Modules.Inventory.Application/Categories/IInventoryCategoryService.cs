using ErpClink.Modules.Inventory.Application.Categories.Models;

namespace ErpClink.Modules.Inventory.Application.Categories;

public interface IInventoryCategoryService
{
    Task<InventoryCategoryDto> CreateAsync(CreateInventoryCategoryRequest request, CancellationToken cancellationToken = default);
    Task<InventoryCategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedInventoryCategoriesResult> SearchAsync(SearchInventoryCategoriesRequest request, CancellationToken cancellationToken = default);
    Task<InventoryCategoryDto> UpdateAsync(Guid id, UpdateInventoryCategoryRequest request, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
