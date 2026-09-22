using ErpClink.Modules.Finance.Domain;

namespace ErpClink.Modules.Finance.Domain.Journals;

public sealed class JournalEntryLine
{
    private JournalEntryLine()
    {
    }

    public Guid Id { get; private set; }
    public Guid JournalEntryId { get; private set; }
    public Guid AccountId { get; private set; }
    public string? Description { get; private set; }
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }
    public int SortOrder { get; private set; }

    public static JournalEntryLine Create(
        Guid journalEntryId,
        Guid accountId,
        string? description,
        decimal debit,
        decimal credit,
        int sortOrder)
    {
        ValidateAmounts(debit, credit);
        return new JournalEntryLine
        {
            Id = Guid.NewGuid(),
            JournalEntryId = journalEntryId,
            AccountId = accountId,
            Description = Normalize(description, 500),
            Debit = Money.Round(debit),
            Credit = Money.Round(credit),
            SortOrder = sortOrder
        };
    }

    public void Update(Guid accountId, string? description, decimal debit, decimal credit, int sortOrder)
    {
        ValidateAmounts(debit, credit);
        AccountId = accountId;
        Description = Normalize(description, 500);
        Debit = Money.Round(debit);
        Credit = Money.Round(credit);
        SortOrder = sortOrder;
    }

    public JournalEntryLine CreateReversalLine(Guid reversalJournalId, int sortOrder)
    {
        ValidateAmounts(Debit, Credit);
        return new JournalEntryLine
        {
            Id = Guid.NewGuid(),
            JournalEntryId = reversalJournalId,
            AccountId = AccountId,
            Description = Description is null ? null : $"Reversal: {Description}",
            Debit = Credit,
            Credit = Debit,
            SortOrder = sortOrder
        };
    }

    internal static void ValidateAmounts(decimal debit, decimal credit)
    {
        Money.EnsureNonNegative(debit, nameof(debit));
        Money.EnsureNonNegative(credit, nameof(credit));

        var hasDebit = debit > 0;
        var hasCredit = credit > 0;
        if (hasDebit == hasCredit)
            throw new InvalidOperationException("Each journal line must have either a debit or a credit amount, not both or neither.");
    }

    private static string? Normalize(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
