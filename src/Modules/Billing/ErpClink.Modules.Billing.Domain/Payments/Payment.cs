using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Billing.Domain.Payments;

public sealed class PaymentCreatedDomainEvent : IDomainEvent
{
    public PaymentCreatedDomainEvent(Guid paymentId, Guid invoiceId, decimal amount, DateTime occurredOnUtc)
    {
        PaymentId = paymentId;
        InvoiceId = invoiceId;
        Amount = amount;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid PaymentId { get; }
    public Guid InvoiceId { get; }
    public decimal Amount { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class PaymentReversedDomainEvent : IDomainEvent
{
    public PaymentReversedDomainEvent(Guid paymentId, Guid invoiceId, DateTime occurredOnUtc)
    {
        PaymentId = paymentId;
        InvoiceId = invoiceId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid PaymentId { get; }
    public Guid InvoiceId { get; }
    public DateTime OccurredOnUtc { get; }
}

/// <summary>
/// Payment captured against a single invoice (V1). Multi-invoice allocations are deferred.
/// Completed payments are never deleted; use <see cref="Reverse"/>.
/// </summary>
public sealed class Payment : AggregateRoot
{
    private Payment()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public string PaymentNumber { get; private set; } = string.Empty;
    public Guid InvoiceId { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = "EGP";
    public PaymentMethod Method { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? Notes { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime? ReversedAtUtc { get; private set; }
    public string? ReversedBy { get; private set; }
    public string? ReversalReason { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public static Payment Capture(
        Guid organizationId,
        Guid branchId,
        string paymentNumber,
        Guid invoiceId,
        DateOnly paymentDate,
        decimal amount,
        string currencyCode,
        PaymentMethod method,
        string? referenceNumber,
        string? notes,
        string? createdBy,
        DateTime utcNow)
    {
        Money.EnsurePositive(amount, nameof(amount));
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        if (currencyCode.Trim().Length != 3)
            throw new ArgumentException("Currency code must be ISO 4217 (3 letters).", nameof(currencyCode));

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            PaymentNumber = paymentNumber.Trim(),
            InvoiceId = invoiceId,
            PaymentDate = paymentDate,
            Amount = Money.Round(amount),
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Method = method,
            ReferenceNumber = Normalize(referenceNumber, 100),
            Notes = Normalize(notes, 1000),
            Status = PaymentStatus.Captured
        };
        payment.SetCreated(createdBy, utcNow);
        payment.RaiseDomainEvent(new PaymentCreatedDomainEvent(payment.Id, invoiceId, payment.Amount, utcNow));
        return payment;
    }

    public void Reverse(string? reason, string? reversedBy, DateTime utcNow)
    {
        if (Status != PaymentStatus.Captured)
            throw new InvalidOperationException("Only captured payments can be reversed.");

        Status = PaymentStatus.Reversed;
        ReversedAtUtc = utcNow;
        ReversedBy = reversedBy;
        ReversalReason = Normalize(reason, 500);
        SetUpdated(reversedBy, utcNow);
        RaiseDomainEvent(new PaymentReversedDomainEvent(Id, InvoiceId, utcNow));
    }

    private static string? Normalize(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
