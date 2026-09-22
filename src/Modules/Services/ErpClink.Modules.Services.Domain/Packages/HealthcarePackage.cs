using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Services.Domain.Packages;

public sealed class HealthcarePackageCreatedDomainEvent : IDomainEvent
{
    public HealthcarePackageCreatedDomainEvent(Guid packageId, string packageCode, DateTime occurredOnUtc)
    {
        PackageId = packageId;
        PackageCode = packageCode;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid PackageId { get; }
    public string PackageCode { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class HealthcarePackageUpdatedDomainEvent : IDomainEvent
{
    public HealthcarePackageUpdatedDomainEvent(Guid packageId, DateTime occurredOnUtc)
    {
        PackageId = packageId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid PackageId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class HealthcarePackageActivatedDomainEvent : IDomainEvent
{
    public HealthcarePackageActivatedDomainEvent(Guid packageId, DateTime occurredOnUtc)
    {
        PackageId = packageId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid PackageId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class HealthcarePackageDeactivatedDomainEvent : IDomainEvent
{
    public HealthcarePackageDeactivatedDomainEvent(Guid packageId, DateTime occurredOnUtc)
    {
        PackageId = packageId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid PackageId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class PackageItemAddedDomainEvent : IDomainEvent
{
    public PackageItemAddedDomainEvent(Guid packageId, Guid itemId, DateTime occurredOnUtc)
    {
        PackageId = packageId;
        ItemId = itemId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid PackageId { get; }
    public Guid ItemId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class PackageItemUpdatedDomainEvent : IDomainEvent
{
    public PackageItemUpdatedDomainEvent(Guid packageId, Guid itemId, DateTime occurredOnUtc)
    {
        PackageId = packageId;
        ItemId = itemId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid PackageId { get; }
    public Guid ItemId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class PackageItemRemovedDomainEvent : IDomainEvent
{
    public PackageItemRemovedDomainEvent(Guid packageId, Guid itemId, DateTime occurredOnUtc)
    {
        PackageId = packageId;
        ItemId = itemId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid PackageId { get; }
    public Guid ItemId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class PackageItem
{
    private PackageItem()
    {
    }

    public Guid Id { get; private set; }
    public Guid PackageId { get; private set; }
    public Guid ServiceId { get; private set; }
    public decimal Quantity { get; private set; }
    public int SortOrder { get; private set; }

    internal static PackageItem Create(Guid packageId, Guid serviceId, decimal quantity, int sortOrder)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        return new PackageItem
        {
            Id = Guid.NewGuid(),
            PackageId = packageId,
            ServiceId = serviceId,
            Quantity = quantity,
            SortOrder = sortOrder
        };
    }

    internal void Update(decimal quantity, int sortOrder)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        Quantity = quantity;
        SortOrder = sortOrder;
    }
}

/// <summary>Bundle of healthcare services (catalog only; purchase/billing is future).</summary>
public sealed class HealthcarePackage : AggregateRoot
{
    private readonly List<PackageItem> _items = [];

    private HealthcarePackage()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string PackageCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public IReadOnlyCollection<PackageItem> Items => _items;

    public static HealthcarePackage Create(
        Guid organizationId,
        string packageCode,
        string name,
        string? description,
        string? createdBy,
        DateTime utcNow)
    {
        var package = new HealthcarePackage
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            PackageCode = Require(packageCode, 64),
            Name = Require(name, 200),
            Description = Normalize(description, 1000),
            IsActive = true
        };
        package.SetCreated(createdBy, utcNow);
        package.RaiseDomainEvent(new HealthcarePackageCreatedDomainEvent(package.Id, package.PackageCode, utcNow));
        return package;
    }

    public void Update(string name, string? description, string? updatedBy, DateTime utcNow)
    {
        EnsureMutable();
        Name = Require(name, 200);
        Description = Normalize(description, 1000);
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new HealthcarePackageUpdatedDomainEvent(Id, utcNow));
    }

    public void Activate(string? updatedBy, DateTime utcNow)
    {
        if (IsActive) return;
        IsActive = true;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new HealthcarePackageActivatedDomainEvent(Id, utcNow));
    }

    public void Deactivate(string? updatedBy, DateTime utcNow)
    {
        if (!IsActive) return;
        IsActive = false;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new HealthcarePackageDeactivatedDomainEvent(Id, utcNow));
    }

    public PackageItem AddItem(Guid serviceId, decimal quantity, int? sortOrder, string? updatedBy, DateTime utcNow)
    {
        EnsureMutable();
        if (_items.Any(i => i.ServiceId == serviceId))
            throw new InvalidOperationException("Service already exists in this package. Update quantity instead.");

        var order = sortOrder ?? (_items.Count == 0 ? 1 : _items.Max(i => i.SortOrder) + 1);
        var item = PackageItem.Create(Id, serviceId, quantity, order);
        _items.Add(item);
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new PackageItemAddedDomainEvent(Id, item.Id, utcNow));
        return item;
    }

    public void UpdateItem(Guid itemId, decimal quantity, int sortOrder, string? updatedBy, DateTime utcNow)
    {
        EnsureMutable();
        var item = _items.SingleOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException("Package item was not found.");
        item.Update(quantity, sortOrder);
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new PackageItemUpdatedDomainEvent(Id, itemId, utcNow));
    }

    public void RemoveItem(Guid itemId, string? updatedBy, DateTime utcNow)
    {
        EnsureMutable();
        var item = _items.SingleOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException("Package item was not found.");
        _items.Remove(item);
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new PackageItemRemovedDomainEvent(Id, itemId, utcNow));
    }

    private void EnsureMutable()
    {
        // Package header/items editable while active; deactivation blocks item edits (catalog frozen).
        if (!IsActive)
            throw new InvalidOperationException("Inactive packages cannot be modified.");
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
