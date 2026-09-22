using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Procurement.Application.Common;

public interface IProcurementDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
