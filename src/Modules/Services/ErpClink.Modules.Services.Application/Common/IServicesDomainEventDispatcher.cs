using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Services.Application.Common;

public interface IServicesDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
