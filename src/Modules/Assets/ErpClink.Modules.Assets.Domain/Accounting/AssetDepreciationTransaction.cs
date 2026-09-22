namespace ErpClink.Modules.Assets.Domain.Accounting;

/// <summary>Immutable posted depreciation fact.</summary>
public sealed class AssetDepreciationTransaction
{
    private AssetDepreciationTransaction()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid AssetId { get; private set; }
    public Guid FinancialProfileId { get; private set; }
    public Guid? ScheduleLineId { get; private set; }
    public string PeriodKey { get; private set; } = string.Empty;
    public DateOnly PeriodStartDate { get; private set; }
    public DateOnly TransactionDate { get; private set; }
    public decimal OpeningNetBookValue { get; private set; }
    public decimal DepreciationAmount { get; private set; }
    public decimal ClosingAccumulatedDepreciation { get; private set; }
    public decimal ClosingNetBookValue { get; private set; }
    public DepreciationMethodCode Method { get; private set; }
    public int CalculationVersion { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public Guid CorrelationId { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? CreatedBy { get; private set; }

    public static AssetDepreciationTransaction Create(
        Guid organizationId,
        Guid branchId,
        Guid assetId,
        Guid financialProfileId,
        Guid? scheduleLineId,
        string periodKey,
        DateOnly periodStartDate,
        DateOnly transactionDate,
        decimal openingNbv,
        decimal depreciationAmount,
        decimal closingAccumulated,
        decimal closingNbv,
        DepreciationMethodCode method,
        int calculationVersion,
        string idempotencyKey,
        Guid correlationId,
        string? description,
        string? createdBy,
        DateTime utcNow)
    {
        Money.EnsureNonNegative(openingNbv, nameof(openingNbv));
        Money.EnsurePositive(depreciationAmount, nameof(depreciationAmount));
        Money.EnsureNonNegative(closingAccumulated, nameof(closingAccumulated));
        Money.EnsureNonNegative(closingNbv, nameof(closingNbv));
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(periodKey);

        return new AssetDepreciationTransaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            AssetId = assetId,
            FinancialProfileId = financialProfileId,
            ScheduleLineId = scheduleLineId,
            PeriodKey = periodKey.Trim(),
            PeriodStartDate = periodStartDate,
            TransactionDate = transactionDate,
            OpeningNetBookValue = Money.Round(openingNbv),
            DepreciationAmount = Money.Round(depreciationAmount),
            ClosingAccumulatedDepreciation = Money.Round(closingAccumulated),
            ClosingNetBookValue = Money.Round(closingNbv),
            Method = method,
            CalculationVersion = calculationVersion,
            IdempotencyKey = idempotencyKey.Trim(),
            CorrelationId = correlationId == Guid.Empty ? Guid.NewGuid() : correlationId,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedAtUtc = utcNow,
            CreatedBy = createdBy
        };
    }
}
