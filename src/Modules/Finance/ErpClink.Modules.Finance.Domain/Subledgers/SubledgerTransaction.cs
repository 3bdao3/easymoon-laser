using ErpClink.BuildingBlocks.Domain.Abstractions;

namespace ErpClink.Modules.Finance.Domain.Subledgers;

/// <summary>
/// Immutable financial subledger movement (AR or AP). Not a General Ledger journal entry.
/// </summary>
public sealed class SubledgerTransaction : AggregateRoot
{
    private SubledgerTransaction()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? BranchId { get; private set; }
    public SubledgerType SubledgerType { get; private set; }
    public Guid PartyId { get; private set; }
    public string SourceModule { get; private set; } = string.Empty;
    public string SourceType { get; private set; } = string.Empty;
    public string SourceId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public DateOnly TransactionDate { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = "EGP";
    public SubledgerDirection Direction { get; private set; }
    public string? Description { get; private set; }
    public string? PartyDisplayNameSnapshot { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public SubledgerTransactionStatus Status { get; private set; }
    public Guid? ReversalOfTransactionId { get; private set; }
    public Guid? ReversedByTransactionId { get; private set; }
    public DateTime PostedAtUtc { get; private set; }
    public string? PostedBy { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    /// <summary>
    /// Signed balance impact: AR Debit+/Credit-; AP Credit+/Debit-.
    /// </summary>
    public decimal SignedBalanceImpact =>
        SubledgerType == SubledgerType.Ar
            ? (Direction == SubledgerDirection.Debit ? Amount : -Amount)
            : (Direction == SubledgerDirection.Credit ? Amount : -Amount);

    public decimal DebitAmount => Direction == SubledgerDirection.Debit ? Amount : 0m;
    public decimal CreditAmount => Direction == SubledgerDirection.Credit ? Amount : 0m;

    public static SubledgerTransaction Post(
        Guid organizationId,
        Guid? branchId,
        SubledgerType subledgerType,
        Guid partyId,
        string sourceModule,
        string sourceType,
        string sourceId,
        string eventType,
        DateOnly transactionDate,
        decimal amount,
        string currencyCode,
        SubledgerDirection direction,
        string? description,
        string? partyDisplayNameSnapshot,
        DateOnly? dueDate,
        Guid correlationId,
        string idempotencyKey,
        string? postedBy,
        DateTime utcNow)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));
        if (partyId == Guid.Empty)
            throw new ArgumentException("PartyId is required.", nameof(partyId));
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModule);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        if (correlationId == Guid.Empty)
            throw new ArgumentException("CorrelationId is required.", nameof(correlationId));

        Money.EnsurePositive(amount, nameof(amount));
        var rounded = Money.Round(amount);
        if (rounded <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        if (currencyCode.Trim().Length != 3)
            throw new ArgumentException("Currency code must be ISO 4217 (3 letters).", nameof(currencyCode));

        var tx = new SubledgerTransaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            SubledgerType = subledgerType,
            PartyId = partyId,
            SourceModule = sourceModule.Trim(),
            SourceType = sourceType.Trim(),
            SourceId = sourceId.Trim(),
            EventType = eventType.Trim(),
            TransactionDate = transactionDate,
            Amount = rounded,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Direction = direction,
            Description = Normalize(description, 1000),
            PartyDisplayNameSnapshot = Normalize(partyDisplayNameSnapshot, 200),
            DueDate = dueDate,
            CorrelationId = correlationId,
            IdempotencyKey = idempotencyKey.Trim(),
            Status = SubledgerTransactionStatus.Posted,
            PostedAtUtc = utcNow,
            PostedBy = postedBy
        };
        tx.SetCreated(postedBy, utcNow);
        tx.RaiseDomainEvent(new SubledgerTransactionPostedDomainEvent(tx.Id, tx.SubledgerType, tx.PartyId, utcNow));
        return tx;
    }

    public SubledgerTransaction CreateReversal(
        string idempotencyKey,
        Guid correlationId,
        string? postedBy,
        DateTime utcNow,
        string? description = null)
    {
        if (Status != SubledgerTransactionStatus.Posted)
            throw new InvalidOperationException("Only posted subledger transactions can be reversed.");
        if (ReversalOfTransactionId is not null)
            throw new InvalidOperationException("Cannot reverse a reversing transaction.");

        var opposite = Direction == SubledgerDirection.Debit
            ? SubledgerDirection.Credit
            : SubledgerDirection.Debit;

        var reversal = Post(
            OrganizationId,
            BranchId,
            SubledgerType,
            PartyId,
            SourceModule,
            SourceType,
            SourceId,
            EventType + "Reversed",
            TransactionDate,
            Amount,
            CurrencyCode,
            opposite,
            description ?? $"Reversal of {Id:N}",
            PartyDisplayNameSnapshot,
            DueDate,
            correlationId,
            idempotencyKey,
            postedBy,
            utcNow);

        reversal.ReversalOfTransactionId = Id;
        Status = SubledgerTransactionStatus.Reversed;
        ReversedByTransactionId = reversal.Id;
        SetUpdated(postedBy, utcNow);
        RaiseDomainEvent(new SubledgerTransactionReversedDomainEvent(Id, reversal.Id, utcNow));
        return reversal;
    }

    private static string? Normalize(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}

public sealed class SubledgerTransactionPostedDomainEvent : IDomainEvent
{
    public SubledgerTransactionPostedDomainEvent(Guid transactionId, SubledgerType subledgerType, Guid partyId, DateTime occurredOnUtc)
    {
        TransactionId = transactionId;
        SubledgerType = subledgerType;
        PartyId = partyId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid TransactionId { get; }
    public SubledgerType SubledgerType { get; }
    public Guid PartyId { get; }
    public DateTime OccurredOnUtc { get; }
}

public sealed class SubledgerTransactionReversedDomainEvent : IDomainEvent
{
    public SubledgerTransactionReversedDomainEvent(Guid originalTransactionId, Guid reversalTransactionId, DateTime occurredOnUtc)
    {
        OriginalTransactionId = originalTransactionId;
        ReversalTransactionId = reversalTransactionId;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid OriginalTransactionId { get; }
    public Guid ReversalTransactionId { get; }
    public DateTime OccurredOnUtc { get; }
}
