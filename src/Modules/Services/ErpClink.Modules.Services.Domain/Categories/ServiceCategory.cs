using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Services.Domain.Categories;

public sealed class ServiceCategoryCreatedDomainEvent : IDomainEvent
{
    public ServiceCategoryCreatedDomainEvent(Guid categoryId, string code, DateTime occurredOnUtc)
    {
        CategoryId = categoryId;
        Code = code;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid CategoryId { get; }
    public string Code { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class ServiceCategoryUpdatedDomainEvent : IDomainEvent
{
    public ServiceCategoryUpdatedDomainEvent(Guid categoryId, DateTime occurredOnUtc)
    {
        CategoryId = categoryId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid CategoryId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class ServiceCategoryActivatedDomainEvent : IDomainEvent
{
    public ServiceCategoryActivatedDomainEvent(Guid categoryId, DateTime occurredOnUtc)
    {
        CategoryId = categoryId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid CategoryId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class ServiceCategoryDeactivatedDomainEvent : IDomainEvent
{
    public ServiceCategoryDeactivatedDomainEvent(Guid categoryId, DateTime occurredOnUtc)
    {
        CategoryId = categoryId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid CategoryId { get; }
    public DateTime OccurredOnUtc { get; }
}

/// <summary>Org-scoped service category (maintainable catalog; not a hardcoded enum).</summary>
public sealed class ServiceCategory : AggregateRoot
{
    private ServiceCategory()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public static ServiceCategory Create(
        Guid organizationId,
        string code,
        string name,
        string? description,
        string? createdBy,
        DateTime utcNow)
    {
        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Code = Require(code, 64),
            Name = Require(name, 200),
            Description = Normalize(description, 500),
            IsActive = true
        };
        category.SetCreated(createdBy, utcNow);
        category.RaiseDomainEvent(new ServiceCategoryCreatedDomainEvent(category.Id, category.Code, utcNow));
        return category;
    }

    public void Update(string name, string? description, string? updatedBy, DateTime utcNow)
    {
        Name = Require(name, 200);
        Description = Normalize(description, 500);
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new ServiceCategoryUpdatedDomainEvent(Id, utcNow));
    }

    public void Activate(string? updatedBy, DateTime utcNow)
    {
        if (IsActive) return;
        IsActive = true;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new ServiceCategoryActivatedDomainEvent(Id, utcNow));
    }

    public void Deactivate(string? updatedBy, DateTime utcNow)
    {
        if (!IsActive) return;
        IsActive = false;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new ServiceCategoryDeactivatedDomainEvent(Id, utcNow));
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
