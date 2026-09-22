using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Billing.Domain.Invoices;

public sealed class InvoiceLine
{
    private InvoiceLine()
    {
    }

    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public InvoiceLineSource Source { get; private set; }
    public Guid? ServiceId { get; private set; }
    public Guid? PackageId { get; private set; }
    public Guid? ServicePriceId { get; private set; }
    public string DescriptionSnapshot { get; private set; } = string.Empty;
    public string? ServiceCodeSnapshot { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineDiscountAmount { get; private set; }
    public decimal LineSubtotal { get; private set; }
    public decimal LineTotal { get; private set; }
    public string CurrencyCode { get; private set; } = "EGP";
    public int SortOrder { get; private set; }

    internal static InvoiceLine Create(
        Guid invoiceId,
        InvoiceLineSource source,
        Guid? serviceId,
        Guid? packageId,
        Guid? servicePriceId,
        string descriptionSnapshot,
        string? serviceCodeSnapshot,
        decimal quantity,
        decimal unitPrice,
        decimal lineDiscountAmount,
        string currencyCode,
        int sortOrder)
    {
        Money.EnsurePositive(quantity, nameof(quantity));
        Money.EnsureNonNegative(unitPrice, nameof(unitPrice));
        Money.EnsureNonNegative(lineDiscountAmount, nameof(lineDiscountAmount));
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptionSnapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        if (currencyCode.Trim().Length != 3)
            throw new ArgumentException("Currency code must be ISO 4217 (3 letters).", nameof(currencyCode));

        var subtotal = Money.Round(quantity * unitPrice);
        if (lineDiscountAmount > subtotal)
            throw new InvalidOperationException("Line discount cannot exceed line subtotal.");

        return new InvoiceLine
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Source = source,
            ServiceId = serviceId,
            PackageId = packageId,
            ServicePriceId = servicePriceId,
            DescriptionSnapshot = descriptionSnapshot.Trim(),
            ServiceCodeSnapshot = string.IsNullOrWhiteSpace(serviceCodeSnapshot) ? null : serviceCodeSnapshot.Trim(),
            Quantity = quantity,
            UnitPrice = Money.Round(unitPrice),
            LineDiscountAmount = Money.Round(lineDiscountAmount),
            LineSubtotal = subtotal,
            LineTotal = Money.Round(subtotal - lineDiscountAmount),
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            SortOrder = sortOrder
        };
    }

    internal void Update(decimal quantity, decimal lineDiscountAmount, int sortOrder)
    {
        Money.EnsurePositive(quantity, nameof(quantity));
        Money.EnsureNonNegative(lineDiscountAmount, nameof(lineDiscountAmount));
        Quantity = quantity;
        LineDiscountAmount = Money.Round(lineDiscountAmount);
        SortOrder = sortOrder;
        Recalculate();
    }

    private void Recalculate()
    {
        LineSubtotal = Money.Round(Quantity * UnitPrice);
        if (LineDiscountAmount > LineSubtotal)
            throw new InvalidOperationException("Line discount cannot exceed line subtotal.");
        LineTotal = Money.Round(LineSubtotal - LineDiscountAmount);
    }
}

public sealed class InvoiceCreatedDomainEvent : IDomainEvent
{
    public InvoiceCreatedDomainEvent(Guid invoiceId, Guid patientId, DateTime occurredOnUtc)
    {
        InvoiceId = invoiceId;
        PatientId = patientId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid InvoiceId { get; }
    public Guid PatientId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class InvoiceIssuedDomainEvent : IDomainEvent
{
    public InvoiceIssuedDomainEvent(Guid invoiceId, string invoiceNumber, DateTime occurredOnUtc)
    {
        InvoiceId = invoiceId;
        InvoiceNumber = invoiceNumber;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid InvoiceId { get; }
    public string InvoiceNumber { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class InvoiceUpdatedDomainEvent : IDomainEvent
{
    public InvoiceUpdatedDomainEvent(Guid invoiceId, DateTime occurredOnUtc)
    {
        InvoiceId = invoiceId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid InvoiceId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class InvoiceVoidedDomainEvent : IDomainEvent
{
    public InvoiceVoidedDomainEvent(Guid invoiceId, DateTime occurredOnUtc)
    {
        InvoiceId = invoiceId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid InvoiceId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class InvoicePaidDomainEvent : IDomainEvent
{
    public InvoicePaidDomainEvent(Guid invoiceId, DateTime occurredOnUtc)
    {
        InvoiceId = invoiceId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid InvoiceId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class InvoicePaymentRegisteredDomainEvent : IDomainEvent
{
    public InvoicePaymentRegisteredDomainEvent(Guid invoiceId, decimal amount, DateTime occurredOnUtc)
    {
        InvoiceId = invoiceId;
        Amount = amount;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid InvoiceId { get; }
    public decimal Amount { get; }
    public DateTime OccurredOnUtc { get; }
}
