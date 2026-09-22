namespace ErpClink.Modules.Assets.Domain.Accounting;

/// <summary>
/// Materialized financial profile for a fixed asset. Operational AssetStatus remains on Asset.
/// Legacy assets without capitalization stay Incomplete and cannot depreciate.
/// </summary>
public sealed class AssetFinancialProfile
{
    private AssetFinancialProfile()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid AssetId { get; private set; }
    public AssetFinancialStatus Status { get; private set; }
    public decimal AcquisitionCost { get; private set; }
    public decimal CapitalizedCost { get; private set; }
    public decimal ResidualValue { get; private set; }
    public DateOnly CapitalizationDate { get; private set; }
    public DateOnly DepreciationStartDate { get; private set; }
    public int UsefulLifeMonths { get; private set; }
    public DepreciationMethodCode DepreciationMethod { get; private set; }
    public int CalculationVersion { get; private set; }
    public decimal AccumulatedDepreciation { get; private set; }
    public decimal NetBookValue { get; private set; }
    public DateOnly? LastDepreciationPeriod { get; private set; }
    public DateOnly? DisposedDate { get; private set; }
    public decimal? DisposalProceeds { get; private set; }
    public decimal? DisposalGainLoss { get; private set; }
    public string? DisposalNotes { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public string? UpdatedBy { get; private set; }

    public decimal DepreciableBase => Money.Round(CapitalizedCost - ResidualValue);
    public decimal RemainingDepreciableAmount => Money.Round(DepreciableBase - AccumulatedDepreciation);

    public static AssetFinancialProfile Capitalize(
        Guid organizationId,
        Guid branchId,
        Guid assetId,
        decimal acquisitionCost,
        decimal capitalizedCost,
        decimal residualValue,
        DateOnly capitalizationDate,
        DateOnly depreciationStartDate,
        int usefulLifeMonths,
        DepreciationMethodCode method,
        string? createdBy,
        DateTime utcNow)
    {
        Money.EnsureNonNegative(acquisitionCost, nameof(acquisitionCost));
        Money.EnsureNonNegative(capitalizedCost, nameof(capitalizedCost));
        Money.EnsureNonNegative(residualValue, nameof(residualValue));
        if (usefulLifeMonths <= 0)
            throw new ArgumentOutOfRangeException(nameof(usefulLifeMonths), "Useful life months must be greater than zero.");
        if (residualValue > capitalizedCost)
            throw new ArgumentOutOfRangeException(nameof(residualValue), "Residual value cannot exceed capitalized cost.");
        if (depreciationStartDate < capitalizationDate)
            throw new ArgumentException("Depreciation start date cannot be before capitalization date.");

        return new AssetFinancialProfile
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            AssetId = assetId,
            Status = AssetFinancialStatus.Capitalized,
            AcquisitionCost = Money.Round(acquisitionCost),
            CapitalizedCost = Money.Round(capitalizedCost),
            ResidualValue = Money.Round(residualValue),
            CapitalizationDate = capitalizationDate,
            DepreciationStartDate = depreciationStartDate,
            UsefulLifeMonths = usefulLifeMonths,
            DepreciationMethod = method,
            CalculationVersion = 1,
            AccumulatedDepreciation = 0m,
            NetBookValue = Money.Round(capitalizedCost),
            CreatedAtUtc = utcNow,
            CreatedBy = createdBy
        };
    }

    public void ApplyDepreciation(decimal amount, DateOnly period, string? updatedBy, DateTime utcNow)
    {
        EnsureCanDepreciate();
        Money.EnsurePositive(amount, nameof(amount));
        if (amount > RemainingDepreciableAmount)
            throw new InvalidOperationException("Depreciation amount exceeds remaining depreciable amount.");

        AccumulatedDepreciation = Money.Round(AccumulatedDepreciation + amount);
        NetBookValue = Money.Round(CapitalizedCost - AccumulatedDepreciation);
        if (NetBookValue < ResidualValue)
            throw new InvalidOperationException("Net book value cannot fall below residual value.");

        LastDepreciationPeriod = period;
        Status = RemainingDepreciableAmount == 0
            ? AssetFinancialStatus.FullyDepreciated
            : AssetFinancialStatus.Depreciating;
        UpdatedAtUtc = utcNow;
        UpdatedBy = updatedBy;
    }

    public void MarkDisposed(
        DateOnly disposedDate,
        decimal? proceeds,
        string? notes,
        string? updatedBy,
        DateTime utcNow)
    {
        if (Status == AssetFinancialStatus.Incomplete)
            throw new InvalidOperationException("Cannot dispose an incomplete financial profile.");
        if (Status == AssetFinancialStatus.Disposed)
            throw new InvalidOperationException("Financial profile is already disposed.");
        if (proceeds is < 0)
            throw new ArgumentOutOfRangeException(nameof(proceeds));

        DisposedDate = disposedDate;
        DisposalProceeds = proceeds is null ? null : Money.Round(proceeds.Value);
        DisposalGainLoss = DisposalProceeds is null ? null : Money.Round(DisposalProceeds.Value - NetBookValue);
        DisposalNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Status = AssetFinancialStatus.Disposed;
        UpdatedAtUtc = utcNow;
        UpdatedBy = updatedBy;
    }

    public void EnsureCanDepreciate()
    {
        if (Status is AssetFinancialStatus.Incomplete or AssetFinancialStatus.Disposed)
            throw new InvalidOperationException("Asset financial profile cannot be depreciated in its current status.");
        if (Status == AssetFinancialStatus.FullyDepreciated)
            throw new InvalidOperationException("Asset is fully depreciated.");
        if (RemainingDepreciableAmount <= 0)
            throw new InvalidOperationException("No remaining depreciable amount.");
    }

    public static DateOnly PeriodKeyToDate(string periodKey)
    {
        if (periodKey.Length != 7 || periodKey[4] != '-')
            throw new ArgumentException("Period key must be YYYY-MM.", nameof(periodKey));
        var year = int.Parse(periodKey[..4]);
        var month = int.Parse(periodKey[5..]);
        return new DateOnly(year, month, 1);
    }

    public static string ToPeriodKey(DateOnly periodStart) => $"{periodStart.Year:D4}-{periodStart.Month:D2}";
}
