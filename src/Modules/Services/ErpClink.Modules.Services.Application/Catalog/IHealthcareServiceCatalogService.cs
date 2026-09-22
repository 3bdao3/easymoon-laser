using ErpClink.Modules.Services.Application.Catalog.Models;

namespace ErpClink.Modules.Services.Application.Catalog;

public interface IHealthcareServiceCatalogService
{
    Task<HealthcareServiceDto> CreateAsync(CreateHealthcareServiceRequest request, CancellationToken cancellationToken = default);
    Task<HealthcareServiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedHealthcareServicesResult> SearchAsync(SearchHealthcareServicesRequest request, CancellationToken cancellationToken = default);
    Task<HealthcareServiceDto> UpdateAsync(Guid id, UpdateHealthcareServiceRequest request, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
