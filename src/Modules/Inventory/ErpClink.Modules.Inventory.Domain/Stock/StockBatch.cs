namespace ErpClink.Modules.Inventory.Domain.Stock;

public sealed class StockBatch
{
    private StockBatch()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public string BatchNumber { get; private set; } = string.Empty;
    public DateOnly ExpiryDate { get; private set; }
    public decimal Quantity { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public static StockBatch Create(
        Guid organizationId,
        Guid warehouseId,
        Guid inventoryItemId,
        string batchNumber,
        DateOnly expiryDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchNumber);
        return new StockBatch
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            WarehouseId = warehouseId,
            InventoryItemId = inventoryItemId,
            BatchNumber = batchNumber.Trim(),
            ExpiryDate = expiryDate,
            Quantity = 0
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
            throw new InvalidOperationException("Batch quantity cannot be negative.");
        Quantity = next;
        return Quantity;
    }
}
