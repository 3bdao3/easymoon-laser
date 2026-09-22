namespace ErpClink.Modules.Finance.Domain.Journals;

/// <summary>Append-only journal audit trail.</summary>
public sealed class JournalEntryHistory
{
    private JournalEntryHistory()
    {
    }

    public Guid Id { get; private set; }
    public Guid JournalEntryId { get; private set; }
    public JournalHistoryEventType EventType { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string? OccurredBy { get; private set; }
    public string? Notes { get; private set; }

    public static JournalEntryHistory Record(
        Guid journalEntryId,
        JournalHistoryEventType eventType,
        DateTime occurredAtUtc,
        string? occurredBy,
        string? notes)
    {
        return new JournalEntryHistory
        {
            Id = Guid.NewGuid(),
            JournalEntryId = journalEntryId,
            EventType = eventType,
            OccurredAtUtc = occurredAtUtc,
            OccurredBy = occurredBy,
            Notes = notes is null or { Length: 0 } ? null : (notes.Length <= 500 ? notes : notes[..500])
        };
    }
}
