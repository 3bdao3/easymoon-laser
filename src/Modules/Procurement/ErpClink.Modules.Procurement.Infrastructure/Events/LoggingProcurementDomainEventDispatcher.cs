using ErpClink.BuildingBlocks.Domain.Abstractions;
using ErpClink.Modules.Procurement.Application.Common;
using ErpClink.Modules.Procurement.Domain.PurchaseOrders;
using ErpClink.Modules.Procurement.Domain.Suppliers;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.Procurement.Infrastructure.Events;

public sealed class ProcurementDomainEventCollector
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

public sealed class LoggingProcurementDomainEventDispatcher : IProcurementDomainEventDispatcher
{
    private readonly ILogger<LoggingProcurementDomainEventDispatcher> _logger;
    private readonly ProcurementDomainEventCollector _collector;

    public LoggingProcurementDomainEventDispatcher(
        ILogger<LoggingProcurementDomainEventDispatcher> logger,
        ProcurementDomainEventCollector collector)
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
                case SupplierCreatedDomainEvent e:
                    _logger.LogInformation("SupplierCreated {SupplierId} {SupplierCode}", e.SupplierId, e.SupplierCode);
                    break;
                case PurchaseOrderSubmittedDomainEvent e:
                    _logger.LogInformation("PurchaseOrderSubmitted {PurchaseOrderId} {Number}", e.PurchaseOrderId, e.PurchaseOrderNumber);
                    break;
                case PurchaseOrderApprovedDomainEvent e:
                    _logger.LogInformation("PurchaseOrderApproved {PurchaseOrderId}", e.PurchaseOrderId);
                    break;
                case PurchaseOrderCancelledDomainEvent e:
                    _logger.LogInformation("PurchaseOrderCancelled {PurchaseOrderId}", e.PurchaseOrderId);
                    break;
                default:
                    _logger.LogInformation("{EventType}", domainEvent.GetType().Name);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
