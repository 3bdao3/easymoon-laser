using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Prescriptions.Application.Common;

public interface IPrescriptionsDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
