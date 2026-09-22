using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Inventory.Domain.Items;

public sealed class InventoryItem : AggregateRoot
{
    private InventoryItem()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string ItemCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public string? Barcode { get; private set; }
    public bool TrackExpiry { get; private set; }
    public decimal MinStockQuantity { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public static InventoryItem Create(
        Guid organizationId,
        string itemCode,
        string name,
        string? description,
        Guid? categoryId,
        string unitOfMeasure,
        string? barcode,
        bool trackExpiry,
        decimal minStockQuantity,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemCode);
        Quantity.EnsureNonNegative(minStockQuantity, nameof(minStockQuantity));

        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ItemCode = itemCode.Trim(),
            Name = Require(name, 200),
            Description = Normalize(description, 500),
            CategoryId = categoryId,
            UnitOfMeasure = Require(unitOfMeasure, 32),
            Barcode = Normalize(barcode, 64),
            TrackExpiry = trackExpiry,
            MinStockQuantity = Quantity.Round(minStockQuantity),
            IsActive = true
        };
        item.SetCreated(createdBy, utcNow);
        return item;
    }

    public void Update(
        string name,
        string? description,
        Guid? categoryId,
        string unitOfMeasure,
        string? barcode,
        bool trackExpiry,
        decimal minStockQuantity,
        string? updatedBy,
        DateTime utcNow)
    {
        Quantity.EnsureNonNegative(minStockQuantity, nameof(minStockQuantity));
        Name = Require(name, 200);
        Description = Normalize(description, 500);
        CategoryId = categoryId;
        UnitOfMeasure = Require(unitOfMeasure, 32);
        Barcode = Normalize(barcode, 64);
        TrackExpiry = trackExpiry;
        MinStockQuantity = Quantity.Round(minStockQuantity);
        SetUpdated(updatedBy, utcNow);
    }

    public void Activate(string? updatedBy, DateTime utcNow)
    {
        if (IsActive) return;
        IsActive = true;
        SetUpdated(updatedBy, utcNow);
    }

    public void Deactivate(string? updatedBy, DateTime utcNow)
    {
        if (!IsActive) return;
        IsActive = false;
        SetUpdated(updatedBy, utcNow);
    }

    public void EnsureBatchRequired(string? batchNumber, DateOnly? expiryDate)
    {
        if (!TrackExpiry) return;
        if (string.IsNullOrWhiteSpace(batchNumber) || expiryDate is null)
            throw new InvalidOperationException("Batch number and expiry date are required for expiry-tracked items.");
    }

    private static string Require(string value, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static string? Normalize(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
