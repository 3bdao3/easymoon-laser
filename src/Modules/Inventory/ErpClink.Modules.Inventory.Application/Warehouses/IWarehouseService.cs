using ErpClink.Modules.Inventory.Application.Warehouses.Models;

namespace ErpClink.Modules.Inventory.Application.Warehouses;

public interface IWarehouseService
{
    Task<WarehouseDto> CreateAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedWarehousesResult> SearchAsync(SearchWarehousesRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseDto> UpdateAsync(Guid id, UpdateWarehouseRequest request, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IWarehouseNumberGenerator
{
    Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
