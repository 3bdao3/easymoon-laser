using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Billing.Application.Common;

public interface IBillingDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
