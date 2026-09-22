using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Procurement.Domain.Suppliers;

public sealed class SupplierCreatedDomainEvent : IDomainEvent
{
    public SupplierCreatedDomainEvent(Guid supplierId, string supplierCode, DateTime occurredOnUtc)
    {
        SupplierId = supplierId;
        SupplierCode = supplierCode;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid SupplierId { get; }
    public string SupplierCode { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class SupplierUpdatedDomainEvent : IDomainEvent
{
    public SupplierUpdatedDomainEvent(Guid supplierId, DateTime occurredOnUtc)
    {
        SupplierId = supplierId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid SupplierId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class SupplierActivatedDomainEvent : IDomainEvent
{
    public SupplierActivatedDomainEvent(Guid supplierId, DateTime occurredOnUtc)
    {
        SupplierId = supplierId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid SupplierId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class SupplierDeactivatedDomainEvent : IDomainEvent
{
    public SupplierDeactivatedDomainEvent(Guid supplierId, DateTime occurredOnUtc)
    {
        SupplierId = supplierId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid SupplierId { get; }
    public DateTime OccurredOnUtc { get; }
}

/// <summary>Organization-level supplier master (no branch scope).</summary>
public sealed class Supplier : AggregateRoot
{
    private Supplier()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string SupplierCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ContactName { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public static Supplier Create(
        Guid organizationId,
        string supplierCode,
        string name,
        string? contactName,
        string? phone,
        string? email,
        string? address,
        string? notes,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(supplierCode);
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            SupplierCode = supplierCode.Trim(),
            Name = Require(name, 200),
            ContactName = Normalize(contactName, 200),
            Phone = Normalize(phone, 32),
            Email = Normalize(email, 256),
            Address = Normalize(address, 500),
            Notes = Normalize(notes, 1000),
            IsActive = true
        };
        supplier.SetCreated(createdBy, utcNow);
        supplier.RaiseDomainEvent(new SupplierCreatedDomainEvent(supplier.Id, supplier.SupplierCode, utcNow));
        return supplier;
    }

    public void Update(
        string name,
        string? contactName,
        string? phone,
        string? email,
        string? address,
        string? notes,
        string? updatedBy,
        DateTime utcNow)
    {
        Name = Require(name, 200);
        ContactName = Normalize(contactName, 200);
        Phone = Normalize(phone, 32);
        Email = Normalize(email, 256);
        Address = Normalize(address, 500);
        Notes = Normalize(notes, 1000);
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new SupplierUpdatedDomainEvent(Id, utcNow));
    }

    public void Activate(string? updatedBy, DateTime utcNow)
    {
        if (IsActive) return;
        IsActive = true;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new SupplierActivatedDomainEvent(Id, utcNow));
    }

    public void Deactivate(string? updatedBy, DateTime utcNow)
    {
        if (!IsActive) return;
        IsActive = false;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new SupplierDeactivatedDomainEvent(Id, utcNow));
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
