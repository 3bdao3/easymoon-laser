using ErpClink.BuildingBlocks.Domain.Abstractions;
using ErpClink.Modules.MedicalVisits.Application.Common;
using ErpClink.Modules.MedicalVisits.Domain.Visits;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.MedicalVisits.Infrastructure.Events;

public sealed class MedicalVisitsDomainEventCollector
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

public sealed class LoggingMedicalVisitsDomainEventDispatcher : IMedicalVisitsDomainEventDispatcher
{
    private readonly ILogger<LoggingMedicalVisitsDomainEventDispatcher> _logger;
    private readonly MedicalVisitsDomainEventCollector _collector;

    public LoggingMedicalVisitsDomainEventDispatcher(
        ILogger<LoggingMedicalVisitsDomainEventDispatcher> logger,
        MedicalVisitsDomainEventCollector collector)
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
                case MedicalVisitStartedDomainEvent e:
                    _logger.LogInformation("MedicalVisitStarted {MedicalVisitId} {VisitNumber}", e.MedicalVisitId, e.VisitNumber);
                    break;
                case MedicalVisitClinicalNotesUpdatedDomainEvent e:
                    _logger.LogInformation("MedicalVisitClinicalNotesUpdated {MedicalVisitId}", e.MedicalVisitId);
                    break;
                case MedicalVisitCompletedDomainEvent e:
                    _logger.LogInformation("MedicalVisitCompleted {MedicalVisitId}", e.MedicalVisitId);
                    break;
                case MedicalVisitCancelledDomainEvent e:
                    _logger.LogInformation("MedicalVisitCancelled {MedicalVisitId}", e.MedicalVisitId);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
