using ErpClink.BuildingBlocks.Domain.Abstractions;
using ErpClink.Modules.Finance.Application.Common;
using ErpClink.Modules.Finance.Domain.Accounts;
using ErpClink.Modules.Finance.Domain.Journals;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.Finance.Infrastructure.Events;

public sealed class FinanceDomainEventCollector
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

public sealed class LoggingFinanceDomainEventDispatcher : IFinanceDomainEventDispatcher
{
    private readonly ILogger<LoggingFinanceDomainEventDispatcher> _logger;
    private readonly FinanceDomainEventCollector _collector;

    public LoggingFinanceDomainEventDispatcher(
        ILogger<LoggingFinanceDomainEventDispatcher> logger,
        FinanceDomainEventCollector collector)
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
                case AccountCreatedDomainEvent e:
                    _logger.LogInformation("AccountCreated {AccountId}", e.AccountId);
                    break;
                case JournalEntryPostedDomainEvent e:
                    _logger.LogInformation("JournalEntryPosted {JournalEntryId} {JournalNumber}", e.JournalEntryId, e.JournalNumber);
                    break;
                case JournalEntryReversedDomainEvent e:
                    _logger.LogInformation("JournalEntryReversed {JournalEntryId} {ReversalJournalId}", e.JournalEntryId, e.ReversalJournalId);
                    break;
                default:
                    _logger.LogInformation("{EventType}", domainEvent.GetType().Name);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
