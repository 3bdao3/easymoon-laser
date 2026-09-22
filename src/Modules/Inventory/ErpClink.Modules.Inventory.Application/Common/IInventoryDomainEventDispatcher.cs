using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Inventory.Application.Common;

public interface IInventoryDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken = default);
}
