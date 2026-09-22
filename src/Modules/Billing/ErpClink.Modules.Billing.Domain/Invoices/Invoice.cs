using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Billing.Domain.Invoices;

/// <summary>
/// Commercial invoice aggregate. Historical charged amounts live on <see cref="InvoiceLine"/> snapshots.
/// Issued invoices must never recalculate amounts from the live Services catalog.
/// </summary>
public sealed class Invoice : AggregateRoot
{
    private readonly List<InvoiceLine> _lines = [];

    private Invoice()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public string? InvoiceNumber { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid? MedicalVisitId { get; private set; }
    public DateOnly InvoiceDate { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public string CurrencyCode { get; private set; } = "EGP";
    public decimal SubTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal OutstandingAmount { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? IssuedAtUtc { get; private set; }
    public string? IssuedBy { get; private set; }
    public DateTime? VoidedAtUtc { get; private set; }
    public string? VoidedBy { get; private set; }
    public string? VoidReason { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public IReadOnlyCollection<InvoiceLine> Lines => _lines;

    public static Invoice CreateDraft(
        Guid organizationId,
        Guid branchId,
        Guid patientId,
        Guid? medicalVisitId,
        DateOnly invoiceDate,
        string currencyCode,
        string? notes,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        if (currencyCode.Trim().Length != 3)
            throw new ArgumentException("Currency code must be ISO 4217 (3 letters).", nameof(currencyCode));

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            PatientId = patientId,
            MedicalVisitId = medicalVisitId,
            InvoiceDate = invoiceDate,
            Status = InvoiceStatus.Draft,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Notes = Normalize(notes, 1000),
            DiscountAmount = 0,
            TaxAmount = 0,
            PaidAmount = 0
        };
        invoice.SetCreated(createdBy, utcNow);
        invoice.RecalculateTotals();
        invoice.RaiseDomainEvent(new InvoiceCreatedDomainEvent(invoice.Id, invoice.PatientId, utcNow));
        return invoice;
    }

    public void UpdateDraftHeader(DateOnly invoiceDate, string? notes, string? updatedBy, DateTime utcNow)
    {
        EnsureDraft();
        InvoiceDate = invoiceDate;
        Notes = Normalize(notes, 1000);
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new InvoiceUpdatedDomainEvent(Id, utcNow));
    }

    public InvoiceLine AddServiceLine(
        Guid serviceId,
        Guid? servicePriceId,
        string descriptionSnapshot,
        string? serviceCodeSnapshot,
        decimal quantity,
        decimal unitPrice,
        decimal lineDiscountAmount,
        string currencyCode,
        int? sortOrder,
        string? updatedBy,
        DateTime utcNow)
    {
        EnsureDraft();
        EnsureCurrency(currencyCode);
        var order = sortOrder ?? NextSortOrder();
        var line = InvoiceLine.Create(
            Id,
            InvoiceLineSource.Service,
            serviceId,
            null,
            servicePriceId,
            descriptionSnapshot,
            serviceCodeSnapshot,
            quantity,
            unitPrice,
            lineDiscountAmount,
            CurrencyCode,
            order);
        _lines.Add(line);
        RecalculateTotals();
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new InvoiceUpdatedDomainEvent(Id, utcNow));
        return line;
    }

    public InvoiceLine AddPackageItemLine(
        Guid serviceId,
        Guid packageId,
        Guid? servicePriceId,
        string descriptionSnapshot,
        string? serviceCodeSnapshot,
        decimal quantity,
        decimal unitPrice,
        decimal lineDiscountAmount,
        string currencyCode,
        int? sortOrder,
        string? updatedBy,
        DateTime utcNow)
    {
        EnsureDraft();
        EnsureCurrency(currencyCode);
        var order = sortOrder ?? NextSortOrder();
        var line = InvoiceLine.Create(
            Id,
            InvoiceLineSource.PackageItem,
            serviceId,
            packageId,
            servicePriceId,
            descriptionSnapshot,
            serviceCodeSnapshot,
            quantity,
            unitPrice,
            lineDiscountAmount,
            CurrencyCode,
            order);
        _lines.Add(line);
        RecalculateTotals();
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new InvoiceUpdatedDomainEvent(Id, utcNow));
        return line;
    }

    public void UpdateLine(Guid lineId, decimal quantity, decimal lineDiscountAmount, int sortOrder, string? updatedBy, DateTime utcNow)
    {
        EnsureDraft();
        var line = _lines.SingleOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOperationException("Invoice line was not found.");
        line.Update(quantity, lineDiscountAmount, sortOrder);
        RecalculateTotals();
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new InvoiceUpdatedDomainEvent(Id, utcNow));
    }

    public void RemoveLine(Guid lineId, string? updatedBy, DateTime utcNow)
    {
        EnsureDraft();
        var line = _lines.SingleOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOperationException("Invoice line was not found.");
        _lines.Remove(line);
        RecalculateTotals();
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new InvoiceUpdatedDomainEvent(Id, utcNow));
    }

    public void SetInvoiceDiscount(decimal discountAmount, string? updatedBy, DateTime utcNow)
    {
        EnsureDraft();
        Money.EnsureNonNegative(discountAmount, nameof(discountAmount));
        DiscountAmount = Money.Round(discountAmount);
        RecalculateTotals();
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new InvoiceUpdatedDomainEvent(Id, utcNow));
    }

    public void Issue(string invoiceNumber, string? issuedBy, DateTime utcNow)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be issued.");
        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot issue an invoice without lines.");
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);

        RecalculateTotals();
        InvoiceNumber = invoiceNumber.Trim();
        Status = InvoiceStatus.Issued;
        IssuedAtUtc = utcNow;
        IssuedBy = issuedBy;
        SetUpdated(issuedBy, utcNow);
        RaiseDomainEvent(new InvoiceIssuedDomainEvent(Id, InvoiceNumber, utcNow));
    }

    public void Void(string? reason, string? voidedBy, DateTime utcNow)
    {
        if (Status is not (InvoiceStatus.Draft or InvoiceStatus.Issued))
            throw new InvalidOperationException("Only draft or issued unpaid invoices can be voided.");
        if (PaidAmount > 0)
            throw new InvalidOperationException("Invoices with payments cannot be voided. Refund/adjustment flow is not implemented in V1.");

        Status = InvoiceStatus.Voided;
        VoidedAtUtc = utcNow;
        VoidedBy = voidedBy;
        VoidReason = Normalize(reason, 500);
        OutstandingAmount = 0;
        SetUpdated(voidedBy, utcNow);
        RaiseDomainEvent(new InvoiceVoidedDomainEvent(Id, utcNow));
    }

    /// <summary>Registers a captured payment against this invoice. Rejects overpayment.</summary>
    public void RegisterPayment(decimal amount, string? updatedBy, DateTime utcNow)
    {
        Money.EnsurePositive(amount, nameof(amount));
        if (Status is InvoiceStatus.Draft or InvoiceStatus.Voided or InvoiceStatus.Paid)
            throw new InvalidOperationException($"Cannot register payment on invoice in status {Status}.");

        var rounded = Money.Round(amount);
        if (rounded > OutstandingAmount)
            throw new InvalidOperationException("Payment amount exceeds outstanding balance. Overpayment is not supported in V1.");

        PaidAmount = Money.Round(PaidAmount + rounded);
        RefreshPaymentStatus();
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new InvoicePaymentRegisteredDomainEvent(Id, rounded, utcNow));
        if (Status == InvoiceStatus.Paid)
            RaiseDomainEvent(new InvoicePaidDomainEvent(Id, utcNow));
    }

    /// <summary>Reverses a previously captured payment amount (net decrease of PaidAmount).</summary>
    public void ReversePayment(decimal amount, string? updatedBy, DateTime utcNow)
    {
        Money.EnsurePositive(amount, nameof(amount));
        if (Status is InvoiceStatus.Draft or InvoiceStatus.Voided)
            throw new InvalidOperationException($"Cannot reverse payment on invoice in status {Status}.");

        var rounded = Money.Round(amount);
        if (rounded > PaidAmount)
            throw new InvalidOperationException("Cannot reverse more than the paid amount.");

        PaidAmount = Money.Round(PaidAmount - rounded);
        RefreshPaymentStatus();
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new InvoicePaymentRegisteredDomainEvent(Id, -rounded, utcNow));
    }

    public void RecalculateTotals()
    {
        SubTotal = Money.Round(_lines.Sum(l => l.LineTotal));
        if (DiscountAmount > SubTotal)
            throw new InvalidOperationException("Invoice discount cannot exceed subtotal.");

        // Tax not enabled in V1 — TaxAmount remains 0 (unresolved product decision).
        TaxAmount = 0;
        TotalAmount = Money.Round(SubTotal - DiscountAmount + TaxAmount);
        OutstandingAmount = Status == InvoiceStatus.Voided
            ? 0
            : Money.Round(TotalAmount - PaidAmount);

        if (OutstandingAmount < 0)
            throw new InvalidOperationException("Outstanding balance cannot be negative.");
    }

    private void RefreshPaymentStatus()
    {
        RecalculateTotals();
        if (PaidAmount == 0)
            Status = InvoiceStatus.Issued;
        else if (OutstandingAmount == 0)
            Status = InvoiceStatus.Paid;
        else
            Status = InvoiceStatus.PartiallyPaid;
    }

    private void EnsureDraft()
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be modified.");
    }

    private void EnsureCurrency(string currencyCode)
    {
        if (!string.Equals(currencyCode.Trim(), CurrencyCode, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Line currency must match invoice currency.");
    }

    private int NextSortOrder() => _lines.Count == 0 ? 1 : _lines.Max(l => l.SortOrder) + 1;

    private static string? Normalize(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
