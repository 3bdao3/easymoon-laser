using ErpClink.BuildingBlocks.Domain.Abstractions;



namespace ErpClink.Modules.Procurement.Domain.PurchaseOrders;



public sealed record PurchaseOrderLineInput(

    Guid? CatalogItemId,

    string DescriptionSnapshot,

    decimal Quantity,

    decimal UnitCost,

    decimal LineDiscountAmount,

    int? SortOrder);



public sealed class PurchaseOrderCreatedDomainEvent : IDomainEvent

{

    public PurchaseOrderCreatedDomainEvent(Guid purchaseOrderId, Guid supplierId, DateTime occurredOnUtc)

    {

        PurchaseOrderId = purchaseOrderId;

        SupplierId = supplierId;

        OccurredOnUtc = occurredOnUtc;

    }



    public Guid PurchaseOrderId { get; }

    public Guid SupplierId { get; }

    public DateTime OccurredOnUtc { get; }

}



public sealed class PurchaseOrderUpdatedDomainEvent : IDomainEvent

{

    public PurchaseOrderUpdatedDomainEvent(Guid purchaseOrderId, DateTime occurredOnUtc)

    {

        PurchaseOrderId = purchaseOrderId;

        OccurredOnUtc = occurredOnUtc;

    }



    public Guid PurchaseOrderId { get; }

    public DateTime OccurredOnUtc { get; }

}



public sealed class PurchaseOrderSubmittedDomainEvent : IDomainEvent

{

    public PurchaseOrderSubmittedDomainEvent(Guid purchaseOrderId, string purchaseOrderNumber, DateTime occurredOnUtc)

    {

        PurchaseOrderId = purchaseOrderId;

        PurchaseOrderNumber = purchaseOrderNumber;

        OccurredOnUtc = occurredOnUtc;

    }



    public Guid PurchaseOrderId { get; }

    public string PurchaseOrderNumber { get; }

    public DateTime OccurredOnUtc { get; }

}



public sealed class PurchaseOrderApprovedDomainEvent : IDomainEvent

{

    public PurchaseOrderApprovedDomainEvent(Guid purchaseOrderId, DateTime occurredOnUtc)

    {

        PurchaseOrderId = purchaseOrderId;

        OccurredOnUtc = occurredOnUtc;

    }



    public Guid PurchaseOrderId { get; }

    public DateTime OccurredOnUtc { get; }

}



public sealed class PurchaseOrderCancelledDomainEvent : IDomainEvent

{

    public PurchaseOrderCancelledDomainEvent(Guid purchaseOrderId, DateTime occurredOnUtc)

    {

        PurchaseOrderId = purchaseOrderId;

        OccurredOnUtc = occurredOnUtc;

    }



    public Guid PurchaseOrderId { get; }

    public DateTime OccurredOnUtc { get; }

}



public sealed class PurchaseOrder : AggregateRoot

{

    private readonly List<PurchaseOrderLine> _lines = [];



    private PurchaseOrder()

    {

    }



    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid BranchId { get; private set; }

    public string PurchaseOrderNumber { get; private set; } = string.Empty;

    public Guid SupplierId { get; private set; }

    public DateOnly OrderDate { get; private set; }

    public PurchaseOrderStatus Status { get; private set; }

    public string CurrencyCode { get; private set; } = "EGP";

    public decimal SubTotal { get; private set; }

    public decimal DiscountAmount { get; private set; }

    public decimal TaxAmount { get; private set; }

    public decimal TotalAmount { get; private set; }

    public string? Notes { get; private set; }

    public DateTime? SubmittedAtUtc { get; private set; }

    public string? SubmittedBy { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public string? ApprovedBy { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public string? CancelledBy { get; private set; }

    public string? CancellationReason { get; private set; }

    public byte[] RowVersion { get; private set; } = null!;



    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines;



    public static PurchaseOrder CreateDraft(

        Guid organizationId,

        Guid branchId,

        string purchaseOrderNumber,

        Guid supplierId,

        DateOnly orderDate,

        string currencyCode,

        string? notes,

        IReadOnlyList<PurchaseOrderLineInput> lines,

        string? createdBy,

        DateTime utcNow)

    {

        ArgumentException.ThrowIfNullOrWhiteSpace(purchaseOrderNumber);

        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);

        if (currencyCode.Trim().Length != 3)

            throw new ArgumentException("Currency code must be ISO 4217 (3 letters).", nameof(currencyCode));



        var po = new PurchaseOrder

        {

            Id = Guid.NewGuid(),

            OrganizationId = organizationId,

            BranchId = branchId,

            PurchaseOrderNumber = purchaseOrderNumber.Trim(),

            SupplierId = supplierId,

            OrderDate = orderDate,

            Status = PurchaseOrderStatus.Draft,

            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),

            Notes = Normalize(notes, 1000),

            DiscountAmount = 0,

            TaxAmount = 0

        };

        po.SetCreated(createdBy, utcNow);



        if (lines is { Count: > 0 })

        {

            foreach (var input in lines)

                po.AddLineInternal(input, createdBy, utcNow, raiseUpdated: false);

        }



        po.RecalculateTotals();

        po.RaiseDomainEvent(new PurchaseOrderCreatedDomainEvent(po.Id, po.SupplierId, utcNow));

        return po;

    }



    public void UpdateDraftHeader(

        Guid supplierId,

        DateOnly orderDate,

        string? notes,

        string? updatedBy,

        DateTime utcNow)

    {

        EnsureDraft();

        SupplierId = supplierId;

        OrderDate = orderDate;

        Notes = Normalize(notes, 1000);

        SetUpdated(updatedBy, utcNow);

        RaiseDomainEvent(new PurchaseOrderUpdatedDomainEvent(Id, utcNow));

    }



    public PurchaseOrderLine AddLine(PurchaseOrderLineInput input, string? updatedBy, DateTime utcNow)

    {

        EnsureDraft();

        var line = AddLineInternal(input, updatedBy, utcNow, raiseUpdated: true);

        return line;

    }



    public void UpdateLine(Guid lineId, decimal quantity, decimal lineDiscountAmount, int sortOrder, string? updatedBy, DateTime utcNow)

    {

        EnsureDraft();

        var line = _lines.SingleOrDefault(l => l.Id == lineId)

            ?? throw new InvalidOperationException("Purchase order line was not found.");

        line.Update(quantity, lineDiscountAmount, sortOrder);

        RecalculateTotals();

        SetUpdated(updatedBy, utcNow);

        RaiseDomainEvent(new PurchaseOrderUpdatedDomainEvent(Id, utcNow));

    }



    public void RemoveLine(Guid lineId, string? updatedBy, DateTime utcNow)

    {

        EnsureDraft();

        var line = _lines.SingleOrDefault(l => l.Id == lineId)

            ?? throw new InvalidOperationException("Purchase order line was not found.");

        _lines.Remove(line);

        RecalculateTotals();

        SetUpdated(updatedBy, utcNow);

        RaiseDomainEvent(new PurchaseOrderUpdatedDomainEvent(Id, utcNow));

    }



    public void SetDiscount(decimal discountAmount, string? updatedBy, DateTime utcNow)

    {

        EnsureDraft();

        Money.EnsureNonNegative(discountAmount, nameof(discountAmount));

        DiscountAmount = Money.Round(discountAmount);

        RecalculateTotals();

        SetUpdated(updatedBy, utcNow);

        RaiseDomainEvent(new PurchaseOrderUpdatedDomainEvent(Id, utcNow));

    }



    public void Submit(string? submittedBy, DateTime utcNow)

    {

        if (Status != PurchaseOrderStatus.Draft)

            throw new InvalidOperationException("Only draft purchase orders can be submitted.");

        if (_lines.Count == 0)

            throw new InvalidOperationException("Cannot submit a purchase order without lines.");



        RecalculateTotals();

        Status = PurchaseOrderStatus.Submitted;

        SubmittedAtUtc = utcNow;

        SubmittedBy = submittedBy;

        SetUpdated(submittedBy, utcNow);

        RaiseDomainEvent(new PurchaseOrderSubmittedDomainEvent(Id, PurchaseOrderNumber, utcNow));

    }



    public void Approve(string? approvedBy, DateTime utcNow)

    {

        if (Status != PurchaseOrderStatus.Submitted)

            throw new InvalidOperationException("Only submitted purchase orders can be approved.");



        Status = PurchaseOrderStatus.Approved;

        ApprovedAtUtc = utcNow;

        ApprovedBy = approvedBy;

        SetUpdated(approvedBy, utcNow);

        RaiseDomainEvent(new PurchaseOrderApprovedDomainEvent(Id, utcNow));

    }



    public void EnsureReceivable()

    {

        if (Status is not (PurchaseOrderStatus.Approved or PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived))

            throw new InvalidOperationException("Purchase order is not receivable in its current status.");

    }



    public void ApplyReceipt(IReadOnlyList<(Guid LineId, decimal QtyDelta)> lines, string? updatedBy, DateTime utcNow)
    {
        EnsureReceivable();
        if (lines is null || lines.Count == 0)
            throw new InvalidOperationException("At least one receipt line is required.");

        foreach (var (lineId, qtyDelta) in lines)
        {
            if (qtyDelta <= 0)
                throw new InvalidOperationException("Receipt quantity delta must be greater than zero.");
            var line = _lines.SingleOrDefault(l => l.Id == lineId)
                ?? throw new InvalidOperationException("Purchase order line was not found.");
            line.ApplyReceiptDelta(qtyDelta);
        }

        RefreshReceivingStatus(updatedBy, utcNow);
    }

    /// <summary>
    /// Compensating decrease after Inventory post failure (no DTC). Does not delete history.
    /// </summary>
    public void ReverseReceipt(IReadOnlyList<(Guid LineId, decimal QtyDelta)> lines, string? updatedBy, DateTime utcNow)
    {
        if (lines is null || lines.Count == 0)
            throw new InvalidOperationException("At least one receipt line is required.");

        foreach (var (lineId, qtyDelta) in lines)
        {
            if (qtyDelta <= 0)
                throw new InvalidOperationException("Receipt reverse quantity must be greater than zero.");
            var line = _lines.SingleOrDefault(l => l.Id == lineId)
                ?? throw new InvalidOperationException("Purchase order line was not found.");
            line.ReverseReceiptDelta(qtyDelta);
        }

        RefreshReceivingStatus(updatedBy, utcNow);
    }

    private void RefreshReceivingStatus(string? updatedBy, DateTime utcNow)
    {
        if (Status is PurchaseOrderStatus.Cancelled or PurchaseOrderStatus.Draft or PurchaseOrderStatus.Submitted)
            throw new InvalidOperationException("Purchase order receiving status cannot be refreshed in its current status.");

        var anyReceived = _lines.Any(l => l.QuantityReceived > 0);
        var anyRemaining = _lines.Any(l => l.QuantityReceived < l.Quantity);
        if (!anyReceived)
            Status = PurchaseOrderStatus.Ordered;
        else
            Status = anyRemaining ? PurchaseOrderStatus.PartiallyReceived : PurchaseOrderStatus.Received;

        SetUpdated(updatedBy, utcNow);
    }



    public void Cancel(string? reason, string? cancelledBy, DateTime utcNow)

    {

        if (Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Submitted or PurchaseOrderStatus.Approved))

            throw new InvalidOperationException("Only draft, submitted, or approved purchase orders can be cancelled.");



        Status = PurchaseOrderStatus.Cancelled;

        CancelledAtUtc = utcNow;

        CancelledBy = cancelledBy;

        CancellationReason = Normalize(reason, 500);

        SetUpdated(cancelledBy, utcNow);

        RaiseDomainEvent(new PurchaseOrderCancelledDomainEvent(Id, utcNow));

    }



    public void RecalculateTotals()

    {

        SubTotal = Money.Round(_lines.Sum(l => l.LineTotal));

        if (DiscountAmount > SubTotal)

            throw new InvalidOperationException("Purchase order discount cannot exceed subtotal.");



        TaxAmount = 0;

        TotalAmount = Money.Round(SubTotal - DiscountAmount + TaxAmount);

    }



    private PurchaseOrderLine AddLineInternal(PurchaseOrderLineInput input, string? updatedBy, DateTime utcNow, bool raiseUpdated)

    {

        var order = input.SortOrder ?? NextSortOrder();

        var line = PurchaseOrderLine.Create(

            Id,

            input.CatalogItemId,

            input.DescriptionSnapshot,

            input.Quantity,

            input.UnitCost,

            input.LineDiscountAmount,

            order);

        _lines.Add(line);

        RecalculateTotals();

        if (raiseUpdated)

        {

            SetUpdated(updatedBy, utcNow);

            RaiseDomainEvent(new PurchaseOrderUpdatedDomainEvent(Id, utcNow));

        }



        return line;

    }



    private void EnsureDraft()

    {

        if (Status != PurchaseOrderStatus.Draft)

            throw new InvalidOperationException("Only draft purchase orders can be modified.");

    }



    private int NextSortOrder() => _lines.Count == 0 ? 1 : _lines.Max(l => l.SortOrder) + 1;



    private static string? Normalize(string? value, int max)

    {

        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim();

        return trimmed.Length <= max ? trimmed : trimmed[..max];

    }

}

