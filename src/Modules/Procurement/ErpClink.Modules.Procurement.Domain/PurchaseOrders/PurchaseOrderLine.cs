namespace ErpClink.Modules.Procurement.Domain.PurchaseOrders;



public sealed class PurchaseOrderLine

{

    private PurchaseOrderLine()

    {

    }



    public Guid Id { get; private set; }

    public Guid PurchaseOrderId { get; private set; }

    public Guid? CatalogItemId { get; private set; }

    public string DescriptionSnapshot { get; private set; } = string.Empty;

    public decimal Quantity { get; private set; }

    public decimal QuantityReceived { get; private set; }

    public decimal UnitCost { get; private set; }

    public decimal LineDiscountAmount { get; private set; }

    public decimal LineSubtotal { get; private set; }

    public decimal LineTotal { get; private set; }

    public int SortOrder { get; private set; }



    internal static PurchaseOrderLine Create(

        Guid purchaseOrderId,

        Guid? catalogItemId,

        string descriptionSnapshot,

        decimal quantity,

        decimal unitCost,

        decimal lineDiscountAmount,

        int sortOrder)

    {

        Money.EnsurePositive(quantity, nameof(quantity));

        Money.EnsureNonNegative(unitCost, nameof(unitCost));

        Money.EnsureNonNegative(lineDiscountAmount, nameof(lineDiscountAmount));

        ArgumentException.ThrowIfNullOrWhiteSpace(descriptionSnapshot);



        var subtotal = Money.Round(quantity * unitCost);

        if (lineDiscountAmount > subtotal)

            throw new InvalidOperationException("Line discount cannot exceed line subtotal.");



        return new PurchaseOrderLine

        {

            Id = Guid.NewGuid(),

            PurchaseOrderId = purchaseOrderId,

            CatalogItemId = catalogItemId,

            DescriptionSnapshot = descriptionSnapshot.Trim(),

            Quantity = quantity,

            QuantityReceived = 0,

            UnitCost = Money.Round(unitCost),

            LineDiscountAmount = Money.Round(lineDiscountAmount),

            LineSubtotal = subtotal,

            LineTotal = Money.Round(subtotal - lineDiscountAmount),

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



    internal void ApplyReceiptDelta(decimal qtyDelta)
    {
        Money.EnsurePositive(qtyDelta, nameof(qtyDelta));
        var next = Money.Round(QuantityReceived + qtyDelta);
        if (next > Quantity)
            throw new InvalidOperationException("Received quantity cannot exceed ordered quantity.");
        QuantityReceived = next;
    }

    internal void ReverseReceiptDelta(decimal qtyDelta)
    {
        Money.EnsurePositive(qtyDelta, nameof(qtyDelta));
        var next = Money.Round(QuantityReceived - qtyDelta);
        if (next < 0)
            throw new InvalidOperationException("Received quantity cannot be negative.");
        QuantityReceived = next;
    }



    private void Recalculate()

    {

        LineSubtotal = Money.Round(Quantity * UnitCost);

        if (LineDiscountAmount > LineSubtotal)

            throw new InvalidOperationException("Line discount cannot exceed line subtotal.");

        LineTotal = Money.Round(LineSubtotal - LineDiscountAmount);

    }

}

