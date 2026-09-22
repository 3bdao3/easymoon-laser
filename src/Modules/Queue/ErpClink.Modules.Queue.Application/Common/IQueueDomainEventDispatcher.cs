using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Queue.Application.Common;

public interface IQueueDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
