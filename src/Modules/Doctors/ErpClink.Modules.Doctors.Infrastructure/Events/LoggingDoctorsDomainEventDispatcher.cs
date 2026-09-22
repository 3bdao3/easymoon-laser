using ErpClink.BuildingBlocks.Domain.Abstractions;
using ErpClink.Modules.Doctors.Application.Common;
using ErpClink.Modules.Doctors.Domain.Clinics;
using ErpClink.Modules.Doctors.Domain.Doctors;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.Doctors.Infrastructure.Events;

public sealed class DoctorsDomainEventCollector
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

public sealed class LoggingDoctorsDomainEventDispatcher : IDoctorsDomainEventDispatcher
{
    private readonly ILogger<LoggingDoctorsDomainEventDispatcher> _logger;
    private readonly DoctorsDomainEventCollector _collector;

    public LoggingDoctorsDomainEventDispatcher(
        ILogger<LoggingDoctorsDomainEventDispatcher> logger,
        DoctorsDomainEventCollector collector)
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
                case DoctorRegisteredDomainEvent e:
                    _logger.LogInformation("DoctorRegistered {DoctorId} {DoctorNumber}", e.DoctorId, e.DoctorNumber);
                    break;
                case DoctorAssignedToClinicDomainEvent e:
                    _logger.LogInformation("DoctorAssignedToClinic {DoctorId} {ClinicId}", e.DoctorId, e.ClinicId);
                    break;
                case ClinicCreatedDomainEvent e:
                    _logger.LogInformation("ClinicCreated {ClinicId} {Code}", e.ClinicId, e.Code);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
