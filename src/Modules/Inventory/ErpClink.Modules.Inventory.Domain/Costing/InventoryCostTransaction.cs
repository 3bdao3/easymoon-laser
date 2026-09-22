namespace ErpClink.Modules.Inventory.Domain.Costing;

/// <summary>
/// Immutable posted inventory cost transaction (not a GL journal).
/// </summary>
public sealed class InventoryCostTransaction
{
    private readonly List<InventoryCostAllocation> _allocations = [];

    private InventoryCostTransaction()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public Guid? StockMovementId { get; private set; }
    public InventoryCostTransactionType TransactionType { get; private set; }
    public DateOnly TransactionDate { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal TotalCost { get; private set; }
    public string SourceModule { get; private set; } = "Inventory";
    public string SourceType { get; private set; } = string.Empty;
    public Guid SourceId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public Guid CorrelationId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public InventoryValuationMethodCode ValuationMethod { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? CreatedBy { get; private set; }

    public IReadOnlyCollection<InventoryCostAllocation> Allocations => _allocations;

    public static InventoryCostTransaction Create(
        Guid organizationId,
        Guid branchId,
        Guid warehouseId,
        Guid inventoryItemId,
        Guid? stockMovementId,
        InventoryCostTransactionType transactionType,
        DateOnly transactionDate,
        decimal quantity,
        decimal unitCost,
        decimal totalCost,
        string sourceType,
        Guid sourceId,
        string eventType,
        Guid correlationId,
        string idempotencyKey,
        string? description,
        InventoryValuationMethodCode valuationMethod,
        string? createdBy,
        DateTime utcNow)
    {
        Domain.Quantity.EnsurePositive(quantity, nameof(quantity));
        InventoryCost.EnsureNonNegativeUnitCost(unitCost, nameof(unitCost));
        Money.EnsureNonNegative(totalCost, nameof(totalCost));
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        if (correlationId == Guid.Empty)
            throw new ArgumentException("CorrelationId is required.", nameof(correlationId));

        var tx = new InventoryCostTransaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            InventoryItemId = inventoryItemId,
            StockMovementId = stockMovementId,
            TransactionType = transactionType,
            TransactionDate = transactionDate,
            Quantity = Domain.Quantity.Round(quantity),
            UnitCost = InventoryCost.RoundUnitCost(unitCost),
            TotalCost = InventoryCost.RoundMoney(totalCost),
            SourceType = sourceType.Trim(),
            SourceId = sourceId,
            EventType = eventType.Trim(),
            CorrelationId = correlationId,
            IdempotencyKey = idempotencyKey.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ValuationMethod = valuationMethod,
            CreatedAtUtc = utcNow,
            CreatedBy = createdBy
        };

        return tx;
    }

    public InventoryCostAllocation AddAllocation(Guid costLayerId, decimal quantity, decimal unitCost, decimal totalCost)
    {
        var allocation = InventoryCostAllocation.Create(Id, costLayerId, quantity, unitCost, totalCost);
        _allocations.Add(allocation);
        return allocation;
    }
}

public sealed class InventoryCostAllocation
{
    private InventoryCostAllocation()
    {
    }

    public Guid Id { get; private set; }
    public Guid CostTransactionId { get; private set; }
    public Guid CostLayerId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal TotalCost { get; private set; }

    public static InventoryCostAllocation Create(
        Guid costTransactionId,
        Guid costLayerId,
        decimal quantity,
        decimal unitCost,
        decimal totalCost)
    {
        Domain.Quantity.EnsurePositive(quantity, nameof(quantity));
        return new InventoryCostAllocation
        {
            Id = Guid.NewGuid(),
            CostTransactionId = costTransactionId,
            CostLayerId = costLayerId,
            Quantity = Domain.Quantity.Round(quantity),
            UnitCost = InventoryCost.RoundUnitCost(unitCost),
            TotalCost = InventoryCost.RoundMoney(totalCost)
        };
    }
}
