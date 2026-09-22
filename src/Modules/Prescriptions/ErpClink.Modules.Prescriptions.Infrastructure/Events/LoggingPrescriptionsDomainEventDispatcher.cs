using ErpClink.BuildingBlocks.Domain.Abstractions;
using ErpClink.Modules.Prescriptions.Application.Common;
using ErpClink.Modules.Prescriptions.Domain.Medications;
using ErpClink.Modules.Prescriptions.Domain.Prescriptions;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.Prescriptions.Infrastructure.Events;

public sealed class PrescriptionsDomainEventCollector
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

public sealed class LoggingPrescriptionsDomainEventDispatcher : IPrescriptionsDomainEventDispatcher
{
    private readonly ILogger<LoggingPrescriptionsDomainEventDispatcher> _logger;
    private readonly PrescriptionsDomainEventCollector _collector;

    public LoggingPrescriptionsDomainEventDispatcher(
        ILogger<LoggingPrescriptionsDomainEventDispatcher> logger,
        PrescriptionsDomainEventCollector collector)
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
                case PrescriptionCreatedDomainEvent e:
                    _logger.LogInformation("PrescriptionCreated {PrescriptionId} {Number}", e.PrescriptionId, e.PrescriptionNumber);
                    break;
                case PrescriptionIssuedDomainEvent e:
                    _logger.LogInformation("PrescriptionIssued {PrescriptionId}", e.PrescriptionId);
                    break;
                case PrescriptionCancelledDomainEvent e:
                    _logger.LogInformation("PrescriptionCancelled {PrescriptionId}", e.PrescriptionId);
                    break;
                case MedicationCreatedDomainEvent e:
                    _logger.LogInformation("MedicationCreated {MedicationId} {Code}", e.MedicationId, e.Code);
                    break;
                case MedicationDeactivatedDomainEvent e:
                    _logger.LogInformation("MedicationDeactivated {MedicationId}", e.MedicationId);
                    break;
                default:
                    _logger.LogInformation("{EventType}", domainEvent.GetType().Name);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
