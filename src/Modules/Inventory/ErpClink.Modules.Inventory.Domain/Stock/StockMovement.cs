namespace ErpClink.Modules.Inventory.Domain.Stock;

public enum StockMovementType
{
    Receipt = 0,
    Issue = 1,
    AdjustmentIn = 2,
    AdjustmentOut = 3
}

public sealed class StockMovement
{
    private StockMovement()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public Guid? BatchId { get; private set; }
    public decimal Quantity { get; private set; }
    public StockMovementType MovementType { get; private set; }
    public string ReferenceType { get; private set; } = string.Empty;
    public Guid ReferenceId { get; private set; }
    public string? Reason { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? CreatedBy { get; private set; }

    public static StockMovement Create(
        Guid organizationId,
        Guid branchId,
        Guid warehouseId,
        Guid inventoryItemId,
        Guid? batchId,
        decimal quantity,
        StockMovementType movementType,
        string referenceType,
        Guid referenceId,
        string? reason,
        decimal balanceAfter,
        string? createdBy,
        DateTime createdAtUtc)
    {
        Domain.Quantity.EnsurePositive(quantity, nameof(quantity));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType);

        return new StockMovement
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            InventoryItemId = inventoryItemId,
            BatchId = batchId,
            Quantity = Domain.Quantity.Round(quantity),
            MovementType = movementType,
            ReferenceType = referenceType.Trim(),
            ReferenceId = referenceId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            BalanceAfter = Domain.Quantity.Round(balanceAfter),
            CreatedAtUtc = createdAtUtc,
            CreatedBy = createdBy
        };
    }
}
