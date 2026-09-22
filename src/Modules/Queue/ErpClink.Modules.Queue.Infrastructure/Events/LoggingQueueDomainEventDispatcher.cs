using ErpClink.BuildingBlocks.Domain.Abstractions;
using ErpClink.Modules.Queue.Application.Common;
using ErpClink.Modules.Queue.Domain.Entries;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.Queue.Infrastructure.Events;

public sealed class QueueDomainEventCollector
{
    private readonly List<IDomainEvent> _events = [];

    public IReadOnlyList<IDomainEvent> Events
    {
        get { lock (_events) return _events.ToList(); }
    }

    public void Add(IDomainEvent domainEvent)
    {
        lock (_events) _events.Add(domainEvent);
    }

    public void Clear()
    {
        lock (_events) _events.Clear();
    }
}

public sealed class LoggingQueueDomainEventDispatcher : IQueueDomainEventDispatcher
{
    private readonly ILogger<LoggingQueueDomainEventDispatcher> _logger;
    private readonly QueueDomainEventCollector _collector;

    public LoggingQueueDomainEventDispatcher(
        ILogger<LoggingQueueDomainEventDispatcher> logger,
        QueueDomainEventCollector collector)
    {
        _logger = logger;
        _collector = collector;
    }

    public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            _collector.Add(domainEvent);
            switch (domainEvent)
            {
                case QueueEntryCheckedInDomainEvent e:
                    _logger.LogInformation("QueueEntryCheckedIn {QueueEntryId} {QueueNumber}", e.QueueEntryId, e.QueueNumber);
                    break;
                case QueueEntryCalledDomainEvent e:
                    _logger.LogInformation("QueueEntryCalled {QueueEntryId}", e.QueueEntryId);
                    break;
                case QueueServiceStartedDomainEvent e:
                    _logger.LogInformation("QueueServiceStarted {QueueEntryId}", e.QueueEntryId);
                    break;
                case QueueEntryCompletedDomainEvent e:
                    _logger.LogInformation("QueueEntryCompleted {QueueEntryId}", e.QueueEntryId);
                    break;
                case QueueEntrySkippedDomainEvent e:
                    _logger.LogInformation("QueueEntrySkipped {QueueEntryId}", e.QueueEntryId);
                    break;
                case QueueEntryCancelledDomainEvent e:
                    _logger.LogInformation("QueueEntryCancelled {QueueEntryId}", e.QueueEntryId);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
