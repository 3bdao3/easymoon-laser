namespace ErpClink.Modules.Inventory.Domain.Costing;

/// <summary>
/// FIFO cost layer. Remaining quantity/value never go negative. Not physically deleted when consumed.
/// </summary>
public sealed class InventoryCostLayer
{
    private InventoryCostLayer()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public Guid? StockBatchId { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public Guid SourceId { get; private set; }
    public DateOnly ReceiptDate { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public decimal OriginalQuantity { get; private set; }
    public decimal RemainingQuantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal OriginalValue { get; private set; }
    public decimal RemainingValue { get; private set; }
    public InventoryCostLayerStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public static InventoryCostLayer Create(
        Guid organizationId,
        Guid branchId,
        Guid warehouseId,
        Guid inventoryItemId,
        Guid? stockBatchId,
        string sourceType,
        Guid sourceId,
        DateOnly receiptDate,
        decimal quantity,
        decimal unitCost,
        DateTime utcNow)
    {
        Quantity.EnsurePositive(quantity, nameof(quantity));
        InventoryCost.EnsureNonNegativeUnitCost(unitCost, nameof(unitCost));
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceType);

        var qty = Quantity.Round(quantity);
        var cost = InventoryCost.RoundUnitCost(unitCost);
        var value = InventoryCost.CalculateTotal(qty, cost);

        return new InventoryCostLayer
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            InventoryItemId = inventoryItemId,
            StockBatchId = stockBatchId,
            SourceType = sourceType.Trim(),
            SourceId = sourceId,
            ReceiptDate = receiptDate,
            CreatedAtUtc = utcNow,
            OriginalQuantity = qty,
            RemainingQuantity = qty,
            UnitCost = cost,
            OriginalValue = value,
            RemainingValue = value,
            Status = InventoryCostLayerStatus.Open
        };
    }

    public decimal Consume(decimal quantity)
    {
        Quantity.EnsurePositive(quantity, nameof(quantity));
        var qty = Quantity.Round(quantity);
        if (qty > RemainingQuantity)
            throw new InvalidOperationException("Cannot consume more than remaining cost-layer quantity.");

        var consumedValue = InventoryCost.CalculateTotal(qty, UnitCost);
        // Last slice: assign remaining value to avoid cumulative rounding drift
        if (Quantity.Round(RemainingQuantity - qty) == 0)
            consumedValue = RemainingValue;

        RemainingQuantity = Quantity.Round(RemainingQuantity - qty);
        RemainingValue = InventoryCost.RoundMoney(RemainingValue - consumedValue);
        if (RemainingQuantity == 0)
        {
            RemainingValue = 0;
            Status = InventoryCostLayerStatus.FullyConsumed;
        }

        return consumedValue;
    }

    public void Restore(decimal quantity, decimal value)
    {
        Quantity.EnsurePositive(quantity, nameof(quantity));
        Money.EnsureNonNegative(value, nameof(value));
        var qty = Quantity.Round(quantity);
        var nextQty = Quantity.Round(RemainingQuantity + qty);
        if (nextQty > OriginalQuantity)
            throw new InvalidOperationException("Cannot restore more than original cost-layer quantity.");

        RemainingQuantity = nextQty;
        RemainingValue = InventoryCost.RoundMoney(RemainingValue + value);
        if (RemainingQuantity > 0 && Status != InventoryCostLayerStatus.Reversed)
            Status = InventoryCostLayerStatus.Open;
    }

    public void MarkReversed()
    {
        if (RemainingQuantity != OriginalQuantity)
            throw new InvalidOperationException("Cannot reverse a cost layer that has been partially consumed.");
        RemainingQuantity = 0;
        RemainingValue = 0;
        Status = InventoryCostLayerStatus.Reversed;
    }
}
