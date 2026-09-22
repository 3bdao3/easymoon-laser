namespace ErpClink.Modules.Inventory.Domain.Stock;

public sealed class StockBalance
{
    private StockBalance()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal InventoryValue { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public static StockBalance Create(
        Guid organizationId,
        Guid branchId,
        Guid warehouseId,
        Guid inventoryItemId)
    {
        return new StockBalance
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            WarehouseId = warehouseId,
            InventoryItemId = inventoryItemId,
            Quantity = 0,
            InventoryValue = 0
        };
    }

    public decimal Increase(decimal delta)
    {
        Domain.Quantity.EnsurePositive(delta, nameof(delta));
        Quantity = Domain.Quantity.Round(Quantity + delta);
        return Quantity;
    }

    public decimal Decrease(decimal delta)
    {
        Domain.Quantity.EnsurePositive(delta, nameof(delta));
        var next = Domain.Quantity.Round(Quantity - delta);
        if (next < 0)
            throw new InvalidOperationException("Stock quantity cannot be negative.");
        Quantity = next;
        return Quantity;
    }

    public void IncreaseValue(decimal delta)
    {
        Domain.Money.EnsureNonNegative(delta, nameof(delta));
        InventoryValue = Domain.Money.Round(InventoryValue + delta);
    }

    public void DecreaseValue(decimal delta)
    {
        Domain.Money.EnsureNonNegative(delta, nameof(delta));
        var next = Domain.Money.Round(InventoryValue - delta);
        if (next < 0)
            throw new InvalidOperationException("Inventory value cannot be negative.");
        InventoryValue = next;
    }

    public decimal? AverageUnitCost =>
        Quantity > 0 ? Domain.InventoryCost.RoundUnitCost(InventoryValue / Quantity) : null;
}
