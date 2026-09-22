using ErpClink.BuildingBlocks.Domain.Abstractions;
using ErpClink.Modules.Patients.Application.Common;
using ErpClink.Modules.Patients.Domain.Patients;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.Patients.Infrastructure.Events;

/// <summary>
/// Minimal in-process dispatcher. Outbox integration is deferred (architecture ADR-012).
/// Captures PatientRegistered for future modules without side effects.
/// </summary>
public sealed class LoggingDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly ILogger<LoggingDomainEventDispatcher> _logger;
    private readonly DomainEventCollector _collector;

    public LoggingDomainEventDispatcher(
        ILogger<LoggingDomainEventDispatcher> logger,
        DomainEventCollector collector)
    {
        _logger = logger;
        _collector = collector;
    }

    public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            _collector.Add(domainEvent);

            if (domainEvent is PatientRegisteredDomainEvent registered)
            {
                _logger.LogInformation(
                    "PatientRegistered: {PatientId} {PatientNumber} Org={OrganizationId} Branch={BranchId}",
                    registered.PatientId,
                    registered.PatientNumber,
                    registered.OrganizationId,
                    registered.BranchId);
            }
        }

        return Task.CompletedTask;
    }
}

public sealed class DomainEventCollector
{
    private readonly List<IDomainEvent> _events = [];

    public IReadOnlyList<IDomainEvent> Events
    {
        get
        {
            lock (_events)
            {
                return _events.ToList();
            }
        }
    }

    public void Add(IDomainEvent domainEvent)
    {
        lock (_events)
        {
            _events.Add(domainEvent);
        }
    }

    public void Clear()
    {
        lock (_events)
        {
            _events.Clear();
        }
    }
}
