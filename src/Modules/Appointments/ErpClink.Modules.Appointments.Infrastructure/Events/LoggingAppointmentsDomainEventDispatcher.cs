using ErpClink.BuildingBlocks.Domain.Abstractions;
using ErpClink.Modules.Appointments.Application.Common;
using ErpClink.Modules.Appointments.Domain.Appointments;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.Appointments.Infrastructure.Events;

public sealed class AppointmentsDomainEventCollector
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

public sealed class LoggingAppointmentsDomainEventDispatcher : IAppointmentsDomainEventDispatcher
{
    private readonly ILogger<LoggingAppointmentsDomainEventDispatcher> _logger;
    private readonly AppointmentsDomainEventCollector _collector;

    public LoggingAppointmentsDomainEventDispatcher(
        ILogger<LoggingAppointmentsDomainEventDispatcher> logger,
        AppointmentsDomainEventCollector collector)
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
                case AppointmentBookedDomainEvent e:
                    _logger.LogInformation("AppointmentBooked {AppointmentId}", e.AppointmentId);
                    break;
                case AppointmentConfirmedDomainEvent e:
                    _logger.LogInformation("AppointmentConfirmed {AppointmentId}", e.AppointmentId);
                    break;
                case AppointmentCancelledDomainEvent e:
                    _logger.LogInformation("AppointmentCancelled {AppointmentId}", e.AppointmentId);
                    break;
                case AppointmentRescheduledDomainEvent e:
                    _logger.LogInformation("AppointmentRescheduled {AppointmentId}", e.AppointmentId);
                    break;
                case AppointmentMarkedNoShowDomainEvent e:
                    _logger.LogInformation("AppointmentMarkedNoShow {AppointmentId}", e.AppointmentId);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
