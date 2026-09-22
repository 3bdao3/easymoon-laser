using ErpClink.BuildingBlocks.Domain.Abstractions;
using ErpClink.Modules.Inventory.Application.Common;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.Inventory.Infrastructure.Events;

public sealed class InventoryDomainEventCollector;

public sealed class LoggingInventoryDomainEventDispatcher : IInventoryDomainEventDispatcher
{
    private readonly ILogger<LoggingInventoryDomainEventDispatcher> _logger;

    public LoggingInventoryDomainEventDispatcher(ILogger<LoggingInventoryDomainEventDispatcher> logger) => _logger = logger;

    public Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var evt in events)
            _logger.LogInformation("Inventory domain event {EventType}", evt.GetType().Name);
        return Task.CompletedTask;
    }
}
