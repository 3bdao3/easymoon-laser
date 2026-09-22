using ErpClink.BuildingBlocks.Domain.Abstractions;
using ErpClink.Modules.Finance.Domain;

namespace ErpClink.Modules.Finance.Domain.Journals;

public sealed class JournalEntry : AggregateRoot
{
    private readonly List<JournalEntryLine> _lines = [];

    private JournalEntry()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public string JournalNumber { get; private set; } = string.Empty;
    public DateOnly JournalDate { get; private set; }
    public string? Description { get; private set; }
    public JournalStatus Status { get; private set; }
    public decimal TotalDebit { get; private set; }
    public decimal TotalCredit { get; private set; }
    public Guid? FiscalPeriodId { get; private set; }
    public Guid? ReversalOfJournalId { get; private set; }
    public Guid? ReversedByJournalId { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }
    public string? PostedBy { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public IReadOnlyCollection<JournalEntryLine> Lines => _lines;

    public static JournalEntry CreateDraft(
        Guid organizationId,
        Guid branchId,
        string journalNumber,
        DateOnly journalDate,
        string? description,
        string? createdBy,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalNumber);
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            JournalNumber = journalNumber.Trim(),
            JournalDate = journalDate,
            Description = Normalize(description, 1000),
            Status = JournalStatus.Draft,
            TotalDebit = 0,
            TotalCredit = 0
        };
        entry.SetCreated(createdBy, utcNow);
        entry.RaiseDomainEvent(new JournalEntryCreatedDomainEvent(entry.Id, utcNow));
        return entry;
    }

    public static JournalEntry CreatePostedReversal(
        Guid organizationId,
        Guid branchId,
        string journalNumber,
        DateOnly journalDate,
        string? description,
        Guid fiscalPeriodId,
        JournalEntry original,
        string? createdBy,
        DateTime utcNow)
    {
        if (original.Status != JournalStatus.Posted)
            throw new InvalidOperationException("Only posted journals can be reversed.");
        if (original.ReversedByJournalId is not null)
            throw new InvalidOperationException("Journal has already been reversed.");

        var entry = CreateDraft(organizationId, branchId, journalNumber, journalDate, description, createdBy, utcNow);
        entry.ReversalOfJournalId = original.Id;
        var order = 1;
        foreach (var line in original._lines.OrderBy(l => l.SortOrder))
        {
            entry._lines.Add(line.CreateReversalLine(entry.Id, order++));
        }

        entry.RecalculateTotals();
        entry.EnsureBalanced();
        entry.Post(fiscalPeriodId, createdBy, utcNow);
        return entry;
    }

    public void UpdateDraft(DateOnly journalDate, string? description, string? updatedBy, DateTime utcNow)
    {
        EnsureDraft();
        JournalDate = journalDate;
        Description = Normalize(description, 1000);
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new JournalEntryUpdatedDomainEvent(Id, utcNow));
    }

    public void ReplaceLines(IReadOnlyList<(Guid AccountId, string? Description, decimal Debit, decimal Credit, int SortOrder)> lines, string? updatedBy, DateTime utcNow)
    {
        EnsureDraft();
        _lines.Clear();
        foreach (var line in lines.OrderBy(l => l.SortOrder))
            _lines.Add(JournalEntryLine.Create(Id, line.AccountId, line.Description, line.Debit, line.Credit, line.SortOrder));
        RecalculateTotals();
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new JournalEntryUpdatedDomainEvent(Id, utcNow));
    }

    public JournalEntryLine AddLine(Guid accountId, string? description, decimal debit, decimal credit, int? sortOrder, string? updatedBy, DateTime utcNow)
    {
        EnsureDraft();
        var order = sortOrder ?? NextSortOrder();
        var line = JournalEntryLine.Create(Id, accountId, description, debit, credit, order);
        _lines.Add(line);
        RecalculateTotals();
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new JournalEntryUpdatedDomainEvent(Id, utcNow));
        return line;
    }

    public void RemoveLine(Guid lineId, string? updatedBy, DateTime utcNow)
    {
        EnsureDraft();
        var line = _lines.SingleOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOperationException("Journal line was not found.");
        _lines.Remove(line);
        RecalculateTotals();
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new JournalEntryUpdatedDomainEvent(Id, utcNow));
    }

    public void RecalculateTotals()
    {
        TotalDebit = Money.Round(_lines.Sum(l => l.Debit));
        TotalCredit = Money.Round(_lines.Sum(l => l.Credit));
    }

    public void EnsureBalanced()
    {
        RecalculateTotals();
        if (_lines.Count < 2)
            throw new InvalidOperationException("A journal entry must have at least two lines.");
        if (TotalDebit != TotalCredit)
            throw new InvalidOperationException("Journal entry is not balanced.");
    }

    public void Post(Guid fiscalPeriodId, string? postedBy, DateTime utcNow)
    {
        if (Status != JournalStatus.Draft)
            throw new InvalidOperationException("Only draft journals can be posted.");
        EnsureBalanced();
        FiscalPeriodId = fiscalPeriodId;
        Status = JournalStatus.Posted;
        PostedAtUtc = utcNow;
        PostedBy = postedBy;
        SetUpdated(postedBy, utcNow);
        RaiseDomainEvent(new JournalEntryPostedDomainEvent(Id, JournalNumber, utcNow));
    }

    public void MarkReversed(Guid reversedByJournalId, string? updatedBy, DateTime utcNow)
    {
        if (Status != JournalStatus.Posted)
            throw new InvalidOperationException("Only posted journals can be marked reversed.");
        if (ReversedByJournalId is not null)
            throw new InvalidOperationException("Journal has already been reversed.");

        ReversedByJournalId = reversedByJournalId;
        Status = JournalStatus.Reversed;
        SetUpdated(updatedBy, utcNow);
        RaiseDomainEvent(new JournalEntryReversedDomainEvent(Id, reversedByJournalId, utcNow));
    }

    private void EnsureDraft()
    {
        if (Status != JournalStatus.Draft)
            throw new InvalidOperationException("Only draft journals can be modified.");
    }

    private int NextSortOrder() => _lines.Count == 0 ? 1 : _lines.Max(l => l.SortOrder) + 1;

    private static string? Normalize(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}

public sealed class JournalEntryCreatedDomainEvent : IDomainEvent
{
    public JournalEntryCreatedDomainEvent(Guid journalEntryId, DateTime occurredOnUtc) => (JournalEntryId, OccurredOnUtc) = (journalEntryId, occurredOnUtc);
    public Guid JournalEntryId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class JournalEntryUpdatedDomainEvent : IDomainEvent
{
    public JournalEntryUpdatedDomainEvent(Guid journalEntryId, DateTime occurredOnUtc) => (JournalEntryId, OccurredOnUtc) = (journalEntryId, occurredOnUtc);
    public Guid JournalEntryId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class JournalEntryPostedDomainEvent : IDomainEvent
{
    public JournalEntryPostedDomainEvent(Guid journalEntryId, string journalNumber, DateTime occurredOnUtc)
    {
        JournalEntryId = journalEntryId;
        JournalNumber = journalNumber;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid JournalEntryId { get; }
    public string JournalNumber { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class JournalEntryReversedDomainEvent : IDomainEvent
{
    public JournalEntryReversedDomainEvent(Guid journalEntryId, Guid reversalJournalId, DateTime occurredOnUtc)
    {
        JournalEntryId = journalEntryId;
        ReversalJournalId = reversalJournalId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid JournalEntryId { get; }
    public Guid ReversalJournalId { get; }
    public DateTime OccurredOnUtc { get; }
}
