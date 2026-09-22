using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Services.Domain.Catalog;

public sealed class HealthcareServiceCreatedDomainEvent : IDomainEvent
{
    public HealthcareServiceCreatedDomainEvent(Guid serviceId, string serviceCode, DateTime occurredOnUtc)
    {
        ServiceId = serviceId;
        ServiceCode = serviceCode;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid ServiceId { get; }
    public string ServiceCode { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class HealthcareServiceUpdatedDomainEvent : IDomainEvent
{
    public HealthcareServiceUpdatedDomainEvent(Guid serviceId, DateTime occurredOnUtc)
    {
        ServiceId = serviceId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid ServiceId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class HealthcareServiceActivatedDomainEvent : IDomainEvent
{
    public HealthcareServiceActivatedDomainEvent(Guid serviceId, DateTime occurredOnUtc)
    {
        ServiceId = serviceId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid ServiceId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class HealthcareServiceDeactivatedDomainEvent : IDomainEvent
{
    public HealthcareServiceDeactivatedDomainEvent(Guid serviceId, DateTime occurredOnUtc)
    {
        ServiceId = serviceId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid ServiceId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class HealthcareServicePriceChangedDomainEvent : IDomainEvent
{
    public HealthcareServicePriceChangedDomainEvent(Guid serviceId, Guid newPriceId, DateTime occurredOnUtc)
    {
        ServiceId = serviceId;
        NewPriceId = newPriceId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid ServiceId { get; }
    public Guid NewPriceId { get; }
    public DateTime OccurredOnUtc { get; }
}

/// <summary>Billable healthcare service catalog entry (MedicalService in product docs).</summary>
public sealed class HealthcareService : AggregateRoot
{
    private readonly List<ServicePrice> _prices = [];

    private HealthcareService()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string ServiceCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? CategoryId { get; private set; }
    public decimal DefaultPrice { get; private set; }
    public string CurrencyCode { get; private set; } = "EGP";
    public int? DurationMinutes { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public IReadOnlyCollection<ServicePrice> Prices => _prices;

    public static HealthcareService Create(
        Guid organizationId,
        string serviceCode,
        string name,
        string? description,
        Guid? categoryId,
        decimal defaultPrice,
        string currencyCode,
        int? durationMinutes,
        string? createdBy,
        DateTime utcNow)
    {
        ValidatePrice(defaultPrice);
        ValidateCurrency(currencyCode);
        if (durationMinutes is < 0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes));

        var service = new HealthcareService
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ServiceCode = Require(serviceCode, 64),
            Name = Require(name, 200),
            Description = Normalize(description, 1000),
            CategoryId = categoryId,
            DefaultPrice = defaultPrice,
            CurrencyCode = NormalizeCurrency(currencyCode),
            DurationMinutes = durationMinutes,
            IsActive = true
        };
        service.SetCreated(createdBy, utcNow);
        var price = ServicePrice.Open(service.Id, defaultPrice, service.CurrencyCode, utcNow);
        service._prices.Add(price);
        service.RaiseDomainEvent(new HealthcareServiceCreatedDomainEvent(service.Id, service.ServiceCode, utcNow));
        return service;
    }

    public void Update(
        string name,
        string? description,
        Guid? categoryId,
        decimal? newPrice,
        string? currencyCode,
        int? durationMinutes,
        string? updatedBy,
        DateTime utcNow)
    {
        Name = Require(name, 200);
        Description = Normalize(description, 1000);
        CategoryId = categoryId;
        if (durationMinutes is < 0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        DurationMinutes = durationMinutes;

        if (newPrice.HasValue)
        {
            ValidatePrice(newPrice.Value);
            var currency = currencyCode is null ? CurrencyCode : NormalizeCurrency(currencyCode);
            if (!string.Equals(currency, CurrencyCode, StringComparison.OrdinalIgnoreCase) && _prices.Count > 0)
                throw new InvalidOperationException("Currency change is not supported in this version.");

            if (newPrice.Value != DefaultPrice || !string.Equals(currency, CurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                CloseCurrentPrice(utcNow);
                CurrencyCode = currency;
                DefaultPrice = newPrice.Value;
                var row = ServicePrice.Open(Id, newPrice.Value, CurrencyCode, utcNow);
                _prices.Add(row);
                RaiseDomainEvent(new HealthcareServicePriceChangedDomainEvent(Id, row.Id, utcNow));
            }
        }
        else if (!string.IsNullOrWhiteSpace(currencyCode))
        {
            CurrencyCode = NormalizeCurrency(currencyCode);
        }

        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new HealthcareServiceUpdatedDomainEvent(Id, utcNow));
    }

    public void Activate(string? updatedBy, DateTime utcNow)
    {
        if (IsActive) return;
        IsActive = true;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new HealthcareServiceActivatedDomainEvent(Id, utcNow));
    }

    public void Deactivate(string? updatedBy, DateTime utcNow)
    {
        if (!IsActive) return;
        IsActive = false;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new HealthcareServiceDeactivatedDomainEvent(Id, utcNow));
    }

    internal ServicePrice? GetCurrentPriceRow() =>
        _prices.FirstOrDefault(p => p.EffectiveToUtc is null);

    private void CloseCurrentPrice(DateTime utcNow)
    {
        var current = GetCurrentPriceRow();
        current?.Close(utcNow);
    }

    private static void ValidatePrice(decimal price)
    {
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
    }

    private static void ValidateCurrency(string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        if (currencyCode.Trim().Length != 3)
            throw new ArgumentException("Currency code must be ISO 4217 (3 letters).");
    }

    private static string NormalizeCurrency(string currencyCode) =>
        Require(currencyCode, 3).ToUpperInvariant();

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

/// <summary>Append-only price history; current row has null EffectiveToUtc.</summary>
public sealed class ServicePrice
{
    private ServicePrice()
    {
    }

    public Guid Id { get; private set; }
    public Guid ServiceId { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = "EGP";
    public DateTime EffectiveFromUtc { get; private set; }
    public DateTime? EffectiveToUtc { get; private set; }

    internal static ServicePrice Open(Guid serviceId, decimal amount, string currencyCode, DateTime utcNow) =>
        new()
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            Amount = amount,
            CurrencyCode = currencyCode,
            EffectiveFromUtc = utcNow
        };

    internal void Close(DateTime utcNow) => EffectiveToUtc = utcNow;
}
