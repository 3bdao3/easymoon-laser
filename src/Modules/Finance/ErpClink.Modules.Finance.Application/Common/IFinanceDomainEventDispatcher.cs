using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Finance.Application.Common;

public interface IFinanceDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
