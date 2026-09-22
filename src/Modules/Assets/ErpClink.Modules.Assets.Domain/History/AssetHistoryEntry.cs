namespace ErpClink.Modules.Assets.Domain.History;

public sealed class AssetHistoryEntry
{
    private AssetHistoryEntry()
    {
    }

    public Guid Id { get; private set; }
    public Guid AssetId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? Notes { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string? OccurredBy { get; private set; }

    public static AssetHistoryEntry Record(
        Guid assetId,
        Guid organizationId,
        string eventType,
        string summary,
        DateTime occurredAtUtc,
        string? occurredBy,
        string? oldValue = null,
        string? newValue = null,
        string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        return new AssetHistoryEntry
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            OrganizationId = organizationId,
            EventType = eventType.Trim(),
            Summary = Truncate(summary.Trim(), 500),
            OldValue = TruncateNullable(oldValue, 500),
            NewValue = TruncateNullable(newValue, 500),
            Notes = TruncateNullable(notes, 1000),
            OccurredAtUtc = occurredAtUtc,
            OccurredBy = string.IsNullOrWhiteSpace(occurredBy) ? null : occurredBy.Trim()
        };
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static string? TruncateNullable(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return Truncate(trimmed, max);
    }
}
