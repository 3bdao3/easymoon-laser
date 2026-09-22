using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Assets.Application.Common;

public interface IAssetsDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken = default);
}
