using ErpClink.BuildingBlocks.Domain.Abstractions;
using ErpClink.Modules.Billing.Application.Common;
using ErpClink.Modules.Billing.Domain.Invoices;
using ErpClink.Modules.Billing.Domain.Payments;
using Microsoft.Extensions.Logging;

namespace ErpClink.Modules.Billing.Infrastructure.Events;

public sealed class BillingDomainEventCollector
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

public sealed class LoggingBillingDomainEventDispatcher : IBillingDomainEventDispatcher
{
    private readonly ILogger<LoggingBillingDomainEventDispatcher> _logger;
    private readonly BillingDomainEventCollector _collector;

    public LoggingBillingDomainEventDispatcher(
        ILogger<LoggingBillingDomainEventDispatcher> logger,
        BillingDomainEventCollector collector)
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
                case InvoiceCreatedDomainEvent e:
                    _logger.LogInformation("InvoiceCreated {InvoiceId} {PatientId}", e.InvoiceId, e.PatientId);
                    break;
                case InvoiceIssuedDomainEvent e:
                    _logger.LogInformation("InvoiceIssued {InvoiceId} {InvoiceNumber}", e.InvoiceId, e.InvoiceNumber);
                    break;
                case InvoicePaidDomainEvent e:
                    _logger.LogInformation("InvoicePaid {InvoiceId}", e.InvoiceId);
                    break;
                case PaymentCreatedDomainEvent e:
                    _logger.LogInformation("PaymentCreated {PaymentId} {InvoiceId} {Amount}", e.PaymentId, e.InvoiceId, e.Amount);
                    break;
                case PaymentReversedDomainEvent e:
                    _logger.LogInformation("PaymentReversed {PaymentId} {InvoiceId}", e.PaymentId, e.InvoiceId);
                    break;
                default:
                    _logger.LogInformation("{EventType}", domainEvent.GetType().Name);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
