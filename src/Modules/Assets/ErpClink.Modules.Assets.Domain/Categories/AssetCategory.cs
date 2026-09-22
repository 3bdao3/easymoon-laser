using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Assets.Domain.Categories;

public sealed class AssetCategory : AggregateRoot
{
    private AssetCategory()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public static AssetCategory Create(
        Guid organizationId,
        string code,
        string name,
        string? description,
        string? createdBy,
        DateTime utcNow)
    {
        var category = new AssetCategory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Code = Require(code, 32),
            Name = Require(name, 200),
            Description = Normalize(description, 500),
            IsActive = true
        };
        category.SetCreated(createdBy, utcNow);
        return category;
    }

    public void Update(string name, string? description, string? updatedBy, DateTime utcNow)
    {
        Name = Require(name, 200);
        Description = Normalize(description, 500);
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

    private static string? Normalize(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
