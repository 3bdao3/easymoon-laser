namespace ErpClink.Modules.Assets.Domain.Accounting;

public sealed class AssetDepreciationScheduleLine
{
    private AssetDepreciationScheduleLine()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid AssetId { get; private set; }
    public Guid FinancialProfileId { get; private set; }
    public int PeriodIndex { get; private set; }
    public string PeriodKey { get; private set; } = string.Empty;
    public DateOnly PeriodStartDate { get; private set; }
    public decimal PlannedAmount { get; private set; }
    public DepreciationScheduleLineStatus Status { get; private set; }
    public Guid? PostedTransactionId { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public static AssetDepreciationScheduleLine CreatePlanned(
        Guid organizationId,
        Guid branchId,
        Guid assetId,
        Guid financialProfileId,
        int periodIndex,
        DateOnly periodStartDate,
        decimal plannedAmount)
    {
        Money.EnsureNonNegative(plannedAmount, nameof(plannedAmount));
        return new AssetDepreciationScheduleLine
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            BranchId = branchId,
            AssetId = assetId,
            FinancialProfileId = financialProfileId,
            PeriodIndex = periodIndex,
            PeriodKey = AssetFinancialProfile.ToPeriodKey(periodStartDate),
            PeriodStartDate = periodStartDate,
            PlannedAmount = Money.Round(plannedAmount),
            Status = DepreciationScheduleLineStatus.Planned
        };
    }

    public void MarkPosted(Guid transactionId, decimal actualAmount)
    {
        if (Status != DepreciationScheduleLineStatus.Planned)
            throw new InvalidOperationException("Only planned schedule lines can be posted.");
        Money.EnsurePositive(actualAmount, nameof(actualAmount));
        Status = DepreciationScheduleLineStatus.Posted;
        PostedTransactionId = transactionId;
        PlannedAmount = Money.Round(actualAmount);
    }
}
