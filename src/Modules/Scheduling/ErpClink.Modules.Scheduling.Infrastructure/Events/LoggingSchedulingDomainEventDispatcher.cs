using ErpClink.BuildingBlocks.Domain.Abstractions;
using ErpClink.Modules.Scheduling.Application.Common;
using ErpClink.Modules.Scheduling.Domain.Schedules;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.Scheduling.Infrastructure.Events;

public sealed class SchedulingDomainEventCollector
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

public sealed class LoggingSchedulingDomainEventDispatcher : ISchedulingDomainEventDispatcher
{
    private readonly ILogger<LoggingSchedulingDomainEventDispatcher> _logger;
    private readonly SchedulingDomainEventCollector _collector;

    public LoggingSchedulingDomainEventDispatcher(
        ILogger<LoggingSchedulingDomainEventDispatcher> logger,
        SchedulingDomainEventCollector collector)
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
                case DoctorScheduleCreatedDomainEvent e:
                    _logger.LogInformation("DoctorScheduleCreated {ScheduleId} {DoctorId}", e.ScheduleId, e.DoctorId);
                    break;
                case DoctorScheduleUpdatedDomainEvent e:
                    _logger.LogInformation("DoctorScheduleUpdated {ScheduleId}", e.ScheduleId);
                    break;
                case DoctorScheduleActivatedDomainEvent e:
                    _logger.LogInformation("DoctorScheduleActivated {ScheduleId}", e.ScheduleId);
                    break;
                case DoctorScheduleDeactivatedDomainEvent e:
                    _logger.LogInformation("DoctorScheduleDeactivated {ScheduleId}", e.ScheduleId);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
