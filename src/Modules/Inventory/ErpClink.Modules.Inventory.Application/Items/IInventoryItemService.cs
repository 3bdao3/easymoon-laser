using ErpClink.Modules.Inventory.Application.Items.Models;

namespace ErpClink.Modules.Inventory.Application.Items;

public interface IInventoryItemService
{
    Task<InventoryItemDto> CreateAsync(CreateInventoryItemRequest request, CancellationToken cancellationToken = default);
    Task<InventoryItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedInventoryItemsResult> SearchAsync(SearchInventoryItemsRequest request, CancellationToken cancellationToken = default);
    Task<InventoryItemDto> UpdateAsync(Guid id, UpdateInventoryItemRequest request, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IInventoryItemNumberGenerator
{
    Task<string> GenerateAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
