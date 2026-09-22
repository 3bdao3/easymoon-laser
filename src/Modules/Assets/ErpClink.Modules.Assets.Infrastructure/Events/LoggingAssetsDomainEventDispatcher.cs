using ErpClink.BuildingBlocks.Domain.Abstractions;
using ErpClink.Modules.Assets.Application.Common;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.Assets.Infrastructure.Events;

public sealed class AssetsDomainEventCollector;

public sealed class LoggingAssetsDomainEventDispatcher : IAssetsDomainEventDispatcher
{
    private readonly ILogger<LoggingAssetsDomainEventDispatcher> _logger;

    public LoggingAssetsDomainEventDispatcher(ILogger<LoggingAssetsDomainEventDispatcher> logger) => _logger = logger;

    public Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var evt in events)
            _logger.LogInformation("Assets domain event {EventType}", evt.GetType().Name);
        return Task.CompletedTask;
    }
}
