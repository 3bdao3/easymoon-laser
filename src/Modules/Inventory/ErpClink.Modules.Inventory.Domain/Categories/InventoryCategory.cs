using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Inventory.Domain.Categories;

public sealed class InventoryCategory : AggregateRoot
{
    private InventoryCategory()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public static InventoryCategory Create(
        Guid organizationId,
        string code,
        string name,
        string? createdBy,
        DateTime utcNow)
    {
        var category = new InventoryCategory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Code = Require(code, 64),
            Name = Require(name, 200),
            IsActive = true
        };
        category.SetCreated(createdBy, utcNow);
        return category;
    }

    public void Update(string name, string? updatedBy, DateTime utcNow)
    {
        Name = Require(name, 200);
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

    private static string Require(string value, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
