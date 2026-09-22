using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Inventory.Domain.GoodsReceipts;

public enum GoodsReceiptStatus
{
    Posted = 0,
    Cancelled = 1
}

public sealed record GoodsReceiptLineInput(
    Guid PurchaseOrderLineId,
    Guid InventoryItemId,
    string DescriptionSnapshot,
    decimal Quantity,
    decimal UnitCostSnapshot,
    string? BatchNumber,
    DateOnly? ExpiryDate,
    Guid? StockBatchId);

public sealed class GoodsReceiptLine
{
    private GoodsReceiptLine()
    {
    }

    public Guid Id { get; private set; }
    public Guid GoodsReceiptId { get; private set; }
    public Guid PurchaseOrderLineId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public string DescriptionSnapshot { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitCostSnapshot { get; private set; }
    public string? BatchNumber { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public Guid? StockBatchId { get; private set; }

    internal static GoodsReceiptLine Create(
        Guid goodsReceiptId,
        Guid purchaseOrderLineId,
        Guid inventoryItemId,
        string descriptionSnapshot,
        decimal quantity,
        decimal unitCostSnapshot,
        string? batchNumber,
        DateOnly? expiryDate,
        Guid? stockBatchId)
    {
        Domain.Quantity.EnsurePositive(quantity, nameof(quantity));
        Money.EnsureNonNegative(unitCostSnapshot, nameof(unitCostSnapshot));
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptionSnapshot);

        return new GoodsReceiptLine
        {
            Id = Guid.NewGuid(),
            GoodsReceiptId = goodsReceiptId,
            PurchaseOrderLineId = purchaseOrderLineId,
            InventoryItemId = inventoryItemId,
            DescriptionSnapshot = descriptionSnapshot.Trim(),
            Quantity = Domain.Quantity.Round(quantity),
            UnitCostSnapshot = Money.Round(unitCostSnapshot),
            BatchNumber = string.IsNullOrWhiteSpace(batchNumber) ? null : batchNumber.Trim(),
            ExpiryDate = expiryDate,
            StockBatchId = stockBatchId
        };
    }
}

public sealed class GoodsReceipt : AggregateRoot
{
    private readonly List<GoodsReceiptLine> _lines = [];

    private GoodsReceipt()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public string ReceiptNumber { get; private set; } = string.Empty;
    public Guid WarehouseId { get; private set; }
    public Guid PurchaseOrderId { get; private set; }
    public Guid SupplierId { get; private set; }
    public DateOnly ReceiptDate { get; private set; }
    public GoodsReceiptStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }
    public string? PostedBy { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancelledBy { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public IReadOnlyCollection<GoodsReceiptLine> Lines => _lines;

    public static GoodsReceipt CreatePosted(
        Guid organizationId,
        Guid branchId,
        string receiptNumber,
        Guid warehouseId,
        Guid purchaseOrderId,
        Guid supplierId,
        DateOnly receiptDate,
        string? notes,
        IReadOnlyList<GoodsReceiptLineInput> lines,
        string? postedBy,
        DateTime postedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(receiptNumber);
        if (lines is null || lines.Count == 0)
            throw new InvalidOperationException("Goods receipt must have at least one line.");

        var receipt = new GoodsReceipt
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            ReceiptNumber = receiptNumber.Trim(),
            WarehouseId = warehouseId,
            PurchaseOrderId = purchaseOrderId,
            SupplierId = supplierId,
            ReceiptDate = receiptDate,
            Status = GoodsReceiptStatus.Posted,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            PostedAtUtc = postedAtUtc,
            PostedBy = postedBy
        };
        receipt.SetCreated(postedBy, postedAtUtc);

        foreach (var line in lines)
        {
            receipt._lines.Add(GoodsReceiptLine.Create(
                receipt.Id,
                line.PurchaseOrderLineId,
                line.InventoryItemId,
                line.DescriptionSnapshot,
                line.Quantity,
                line.UnitCostSnapshot,
                line.BatchNumber,
                line.ExpiryDate,
                line.StockBatchId));
        }

        return receipt;
    }

    public void Cancel(string? reason, string? cancelledBy, DateTime utcNow)
    {
        if (Status != GoodsReceiptStatus.Posted)
            throw new InvalidOperationException("Only posted goods receipts can be cancelled.");

        Status = GoodsReceiptStatus.Cancelled;
        CancelledAtUtc = utcNow;
        CancelledBy = cancelledBy;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        SetUpdated(cancelledBy, utcNow);
    }
}
