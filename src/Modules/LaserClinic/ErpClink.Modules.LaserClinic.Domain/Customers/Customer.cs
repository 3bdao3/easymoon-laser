using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.LaserClinic.Domain.Customers;

public sealed class Customer : AggregateRoot
{
    private Customer()
    {
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public int? Age { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>Active pulse package size (1000 / 5000), null when none.</summary>
    public int? PulsePackageTotal { get; private set; }

    /// <summary>Remaining pulses in the active package.</summary>
    public int? PulsePackageRemaining { get; private set; }

    /// <summary>Agreed package price (e.g. 1000); remaining money = this − sum of session payments.</summary>
    public decimal? PackagePriceTotal { get; private set; }

    /// <summary>Agreed package time budget in minutes (e.g. 30); remaining = this − attended session minutes.</summary>
    public int? PackageDurationTotalMinutes { get; private set; }

    public static Customer Create(
        string fullName,
        string phoneNumber,
        int? age,
        string? notes,
        string? userId,
        DateTime utcNow)
    {
        var name = NormalizeName(fullName);
        var phone = NormalizePhone(phoneNumber);
        ValidateAge(age);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = name,
            PhoneNumber = phone,
            Age = age,
            Notes = NormalizeOptional(notes),
            IsActive = true
        };
        customer.SetCreated(userId, utcNow);
        return customer;
    }

    public void Update(string fullName, string phoneNumber, int? age, string? notes, string? userId, DateTime utcNow)
    {
        FullName = NormalizeName(fullName);
        PhoneNumber = NormalizePhone(phoneNumber);
        ValidateAge(age);
        Age = age;
        Notes = NormalizeOptional(notes);
        SetUpdated(userId, utcNow);
    }

    public void Activate(string? userId, DateTime utcNow)
    {
        IsActive = true;
        SetUpdated(userId, utcNow);
    }

    public void Deactivate(string? userId, DateTime utcNow)
    {
        IsActive = false;
        SetUpdated(userId, utcNow);
    }

    /// <summary>
    /// Opens a new package when none / depleted; otherwise keeps the current remaining balance.
    /// </summary>
    public void EnsurePulsePackage(
        int packageSize,
        string? userId,
        DateTime utcNow,
        int? packageDurationMinutes = null,
        decimal? packagePrice = null)
    {
        if (packageSize is not (1000 or 5000))
            throw new ArgumentOutOfRangeException(nameof(packageSize), "Pulse package must be 1000 or 5000.");

        if (PulsePackageRemaining is null or <= 0)
        {
            PulsePackageTotal = packageSize;
            PulsePackageRemaining = packageSize;
            PackageDurationTotalMinutes = packageDurationMinutes is > 0 ? packageDurationMinutes : null;
            PackagePriceTotal = packagePrice is >= 0 ? packagePrice : null;
            SetUpdated(userId, utcNow);
            return;
        }

        if (PackageDurationTotalMinutes is null && packageDurationMinutes is > 0)
        {
            PackageDurationTotalMinutes = packageDurationMinutes;
            SetUpdated(userId, utcNow);
        }

        if (PackagePriceTotal is null && packagePrice is >= 0)
        {
            PackagePriceTotal = packagePrice;
            SetUpdated(userId, utcNow);
        }
    }

    public void SetPackagePrice(decimal packagePrice, string? userId, DateTime utcNow)
    {
        if (packagePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(packagePrice));
        PackagePriceTotal = packagePrice;
        SetUpdated(userId, utcNow);
    }

    public void SetPackageDurationTotal(int minutes, string? userId, DateTime utcNow)
    {
        if (minutes < 1)
            throw new ArgumentOutOfRangeException(nameof(minutes));
        PackageDurationTotalMinutes = minutes;
        SetUpdated(userId, utcNow);
    }

    public void ConsumePulses(int pulses, string? userId, DateTime utcNow)
    {
        if (pulses < 1)
            throw new ArgumentOutOfRangeException(nameof(pulses), "Pulses consumed must be at least 1.");
        if (PulsePackageRemaining is null or <= 0)
            throw new InvalidOperationException("No active pulse package.");
        if (pulses > PulsePackageRemaining.Value)
            throw new InvalidOperationException("Not enough remaining pulses.");

        PulsePackageRemaining -= pulses;
        SetUpdated(userId, utcNow);
    }

    /// <summary>Restore pulses (e.g. when editing/removing a prior consumption).</summary>
    public void RestorePulses(int pulses, string? userId, DateTime utcNow)
    {
        if (pulses < 1)
            return;
        if (PulsePackageTotal is null)
            return;

        PulsePackageRemaining = Math.Min(
            PulsePackageTotal.Value,
            (PulsePackageRemaining ?? 0) + pulses);
        SetUpdated(userId, utcNow);
    }

    private static string NormalizeName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Customer name is required.", nameof(fullName));
        return fullName.Trim();
    }

    private static string NormalizePhone(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));
        return phoneNumber.Trim();
    }

    private static void ValidateAge(int? age)
    {
        if (age is < 1 or > 120)
            throw new ArgumentOutOfRangeException(nameof(age), "Age must be between 1 and 120.");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
