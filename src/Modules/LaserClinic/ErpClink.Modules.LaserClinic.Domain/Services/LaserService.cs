using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.LaserClinic.Domain.Services;

public sealed class LaserService : AggregateRoot
{
    private LaserService()
    {
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int MinDurationMinutes { get; private set; }
    public int MaxDurationMinutes { get; private set; }
    public bool IsActive { get; private set; }
    public int DisplayOrder { get; private set; }
    public string? Notes { get; private set; }

    public static LaserService Create(
        string name,
        int minDurationMinutes,
        int maxDurationMinutes,
        int displayOrder,
        string? notes,
        string? userId,
        DateTime utcNow)
    {
        ValidateDurations(minDurationMinutes, maxDurationMinutes);
        var entity = new LaserService
        {
            Id = Guid.NewGuid(),
            Name = NormalizeName(name),
            MinDurationMinutes = minDurationMinutes,
            MaxDurationMinutes = maxDurationMinutes,
            DisplayOrder = displayOrder,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            IsActive = true
        };
        entity.SetCreated(userId, utcNow);
        return entity;
    }

    public void Update(
        string name,
        int minDurationMinutes,
        int maxDurationMinutes,
        int displayOrder,
        string? notes,
        string? userId,
        DateTime utcNow)
    {
        ValidateDurations(minDurationMinutes, maxDurationMinutes);
        Name = NormalizeName(name);
        MinDurationMinutes = minDurationMinutes;
        MaxDurationMinutes = maxDurationMinutes;
        DisplayOrder = displayOrder;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        SetUpdated(userId, utcNow);
    }

    public void SetActive(bool isActive, string? userId, DateTime utcNow)
    {
        IsActive = isActive;
        SetUpdated(userId, utcNow);
    }

    /// <summary>Recommended booking duration in minutes (backend source of truth).</summary>
    public int GetRecommendedDurationMinutes() =>
        MinDurationMinutes == MaxDurationMinutes ? MinDurationMinutes : MaxDurationMinutes;

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Service name is required.", nameof(name));
        return name.Trim();
    }

    private static void ValidateDurations(int min, int max)
    {
        if (min <= 0)
            throw new ArgumentOutOfRangeException(nameof(min), "Min duration must be positive.");
        if (max < min)
            throw new ArgumentOutOfRangeException(nameof(max), "Max duration must be >= min duration.");
    }
}
