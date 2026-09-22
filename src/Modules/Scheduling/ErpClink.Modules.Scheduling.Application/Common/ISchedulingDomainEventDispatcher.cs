using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Scheduling.Application.Common;

public interface ISchedulingDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
