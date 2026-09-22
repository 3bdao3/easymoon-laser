namespace ErpClink.Modules.Assets.Domain.Accounting;

/// <summary>Immutable asset financial event history (capitalization, depreciation, disposal, correction).</summary>
public sealed class AssetFinancialTransaction
{
    private AssetFinancialTransaction()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid AssetId { get; private set; }
    public Guid FinancialProfileId { get; private set; }
    public AssetFinancialEventType EventType { get; private set; }
    public DateOnly TransactionDate { get; private set; }
    public decimal Amount { get; private set; }
    public decimal? OpeningNetBookValue { get; private set; }
    public decimal? ClosingNetBookValue { get; private set; }
    public decimal? AccumulatedDepreciation { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public Guid SourceId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public Guid CorrelationId { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? CreatedBy { get; private set; }

    public static AssetFinancialTransaction Create(
        Guid organizationId,
        Guid branchId,
        Guid assetId,
        Guid financialProfileId,
        AssetFinancialEventType eventType,
        DateOnly transactionDate,
        decimal amount,
        decimal? openingNbv,
        decimal? closingNbv,
        decimal? accumulatedDepreciation,
        string sourceType,
        Guid sourceId,
        string idempotencyKey,
        Guid correlationId,
        string? description,
        string? createdBy,
        DateTime utcNow)
    {
        Money.EnsureNonNegative(amount, nameof(amount));
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        return new AssetFinancialTransaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            AssetId = assetId,
            FinancialProfileId = financialProfileId,
            EventType = eventType,
            TransactionDate = transactionDate,
            Amount = Money.Round(amount),
            OpeningNetBookValue = openingNbv is null ? null : Money.Round(openingNbv.Value),
            ClosingNetBookValue = closingNbv is null ? null : Money.Round(closingNbv.Value),
            AccumulatedDepreciation = accumulatedDepreciation is null ? null : Money.Round(accumulatedDepreciation.Value),
            SourceType = sourceType.Trim(),
            SourceId = sourceId,
            IdempotencyKey = idempotencyKey.Trim(),
            CorrelationId = correlationId == Guid.Empty ? Guid.NewGuid() : correlationId,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedAtUtc = utcNow,
            CreatedBy = createdBy
        };
    }
}
