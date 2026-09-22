using ErpClink.Modules.Procurement.Application.Suppliers.Models;

namespace ErpClink.Modules.Procurement.Application.Suppliers;

public interface ISupplierService
{
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupplierDto?> GetByCodeAsync(string supplierCode, CancellationToken cancellationToken = default);
    Task<PagedSuppliersResult> SearchAsync(SearchSuppliersRequest request, CancellationToken cancellationToken = default);
    Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, byte[]? rowVersion, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, byte[]? rowVersion, CancellationToken cancellationToken = default);
}

public interface ISupplierNumberGenerator
{
    Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
