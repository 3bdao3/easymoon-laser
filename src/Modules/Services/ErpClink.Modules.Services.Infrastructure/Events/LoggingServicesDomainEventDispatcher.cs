using ErpClink.BuildingBlocks.Domain.Abstractions;
using ErpClink.Modules.Services.Application.Common;
using ErpClink.Modules.Services.Domain.Catalog;
using ErpClink.Modules.Services.Domain.Categories;
using ErpClink.Modules.Services.Domain.Packages;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.Services.Infrastructure.Events;

public sealed class ServicesDomainEventCollector
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

public sealed class LoggingServicesDomainEventDispatcher : IServicesDomainEventDispatcher
{
    private readonly ILogger<LoggingServicesDomainEventDispatcher> _logger;
    private readonly ServicesDomainEventCollector _collector;

    public LoggingServicesDomainEventDispatcher(
        ILogger<LoggingServicesDomainEventDispatcher> logger,
        ServicesDomainEventCollector collector)
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
                case HealthcareServiceCreatedDomainEvent e:
                    _logger.LogInformation("HealthcareServiceCreated {ServiceId} {ServiceCode}", e.ServiceId, e.ServiceCode);
                    break;
                case HealthcareServiceUpdatedDomainEvent e:
                    _logger.LogInformation("HealthcareServiceUpdated {ServiceId}", e.ServiceId);
                    break;
                case HealthcareServiceActivatedDomainEvent e:
                    _logger.LogInformation("HealthcareServiceActivated {ServiceId}", e.ServiceId);
                    break;
                case HealthcareServiceDeactivatedDomainEvent e:
                    _logger.LogInformation("HealthcareServiceDeactivated {ServiceId}", e.ServiceId);
                    break;
                case HealthcareServicePriceChangedDomainEvent e:
                    _logger.LogInformation("HealthcareServicePriceChanged {ServiceId} {PriceId}", e.ServiceId, e.NewPriceId);
                    break;
                case ServiceCategoryCreatedDomainEvent e:
                    _logger.LogInformation("ServiceCategoryCreated {CategoryId} {Code}", e.CategoryId, e.Code);
                    break;
                case ServiceCategoryUpdatedDomainEvent e:
                    _logger.LogInformation("ServiceCategoryUpdated {CategoryId}", e.CategoryId);
                    break;
                case ServiceCategoryActivatedDomainEvent e:
                    _logger.LogInformation("ServiceCategoryActivated {CategoryId}", e.CategoryId);
                    break;
                case ServiceCategoryDeactivatedDomainEvent e:
                    _logger.LogInformation("ServiceCategoryDeactivated {CategoryId}", e.CategoryId);
                    break;
                case HealthcarePackageCreatedDomainEvent e:
                    _logger.LogInformation("HealthcarePackageCreated {PackageId} {PackageCode}", e.PackageId, e.PackageCode);
                    break;
                case HealthcarePackageUpdatedDomainEvent e:
                    _logger.LogInformation("HealthcarePackageUpdated {PackageId}", e.PackageId);
                    break;
                case HealthcarePackageActivatedDomainEvent e:
                    _logger.LogInformation("HealthcarePackageActivated {PackageId}", e.PackageId);
                    break;
                case HealthcarePackageDeactivatedDomainEvent e:
                    _logger.LogInformation("HealthcarePackageDeactivated {PackageId}", e.PackageId);
                    break;
                case PackageItemAddedDomainEvent e:
                    _logger.LogInformation("PackageItemAdded {PackageId} {ItemId}", e.PackageId, e.ItemId);
                    break;
                case PackageItemUpdatedDomainEvent e:
                    _logger.LogInformation("PackageItemUpdated {PackageId} {ItemId}", e.PackageId, e.ItemId);
                    break;
                case PackageItemRemovedDomainEvent e:
                    _logger.LogInformation("PackageItemRemoved {PackageId} {ItemId}", e.PackageId, e.ItemId);
                    break;
                default:
                    _logger.LogInformation("{EventType}", domainEvent.GetType().Name);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
