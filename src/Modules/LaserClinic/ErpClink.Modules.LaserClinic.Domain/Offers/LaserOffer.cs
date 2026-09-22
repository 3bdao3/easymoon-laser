using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.LaserClinic.Domain.Offers;

public sealed class LaserOffer : AggregateRoot
{
    private LaserOffer()
    {
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public DateOnly? ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public bool IsActive { get; private set; }
    public int DisplayOrder { get; private set; }

    public static LaserOffer Create(
        string title,
        string? description,
        decimal price,
        DateOnly? validFrom,
        DateOnly? validTo,
        int displayOrder,
        string? userId,
        DateTime utcNow)
    {
        Validate(price, validFrom, validTo);
        var entity = new LaserOffer
        {
            Id = Guid.NewGuid(),
            Title = NormalizeTitle(title),
            Description = NormalizeOptional(description),
            Price = price,
            ValidFrom = validFrom,
            ValidTo = validTo,
            DisplayOrder = displayOrder,
            IsActive = true
        };
        entity.SetCreated(userId, utcNow);
        return entity;
    }

    public void Update(
        string title,
        string? description,
        decimal price,
        DateOnly? validFrom,
        DateOnly? validTo,
        int displayOrder,
        string? userId,
        DateTime utcNow)
    {
        Validate(price, validFrom, validTo);
        Title = NormalizeTitle(title);
        Description = NormalizeOptional(description);
        Price = price;
        ValidFrom = validFrom;
        ValidTo = validTo;
        DisplayOrder = displayOrder;
        SetUpdated(userId, utcNow);
    }

    public void SetActive(bool isActive, string? userId, DateTime utcNow)
    {
        IsActive = isActive;
        SetUpdated(userId, utcNow);
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Offer title is required.", nameof(title));
        return title.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Validate(decimal price, DateOnly? validFrom, DateOnly? validTo)
    {
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        if (validFrom is not null && validTo is not null && validTo < validFrom)
            throw new ArgumentException("Valid to must be on or after valid from.", nameof(validTo));
    }
}
