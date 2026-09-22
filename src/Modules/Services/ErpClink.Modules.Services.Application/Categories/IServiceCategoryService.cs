using ErpClink.Modules.Services.Application.Categories.Models;

namespace ErpClink.Modules.Services.Application.Categories;

public interface IServiceCategoryService
{
    Task<ServiceCategoryDto> CreateAsync(CreateServiceCategoryRequest request, CancellationToken cancellationToken = default);
    Task<ServiceCategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedServiceCategoriesResult> SearchAsync(SearchServiceCategoriesRequest request, CancellationToken cancellationToken = default);
    Task<ServiceCategoryDto> UpdateAsync(Guid id, UpdateServiceCategoryRequest request, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
