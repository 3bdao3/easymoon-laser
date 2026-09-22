using ErpClink.Modules.Services.Application.Packages.Models;

namespace ErpClink.Modules.Services.Application.Packages;

public interface IHealthcarePackageService
{
    Task<HealthcarePackageDto> CreateAsync(CreateHealthcarePackageRequest request, CancellationToken cancellationToken = default);
    Task<HealthcarePackageDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedHealthcarePackagesResult> SearchAsync(SearchHealthcarePackagesRequest request, CancellationToken cancellationToken = default);
    Task<HealthcarePackageDto> UpdateAsync(Guid id, UpdateHealthcarePackageRequest request, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<HealthcarePackageDto> AddItemAsync(Guid id, PackageItemInput request, CancellationToken cancellationToken = default);
    Task<HealthcarePackageDto> UpdateItemAsync(Guid id, Guid itemId, UpdatePackageItemRequest request, CancellationToken cancellationToken = default);
    Task<HealthcarePackageDto> RemoveItemAsync(Guid id, Guid itemId, byte[]? rowVersion, CancellationToken cancellationToken = default);
}
