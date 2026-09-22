using ErpClink.Modules.Assets.Domain.Accounting;

namespace ErpClink.Modules.Assets.Application.Accounting;

public sealed record PeriodDepreciationRequest(
    decimal CapitalizedCost,
    decimal ResidualValue,
    decimal AccumulatedDepreciation,
    int UsefulLifeMonths,
    int PeriodIndex,
    int PostedPeriodCount,
    decimal PlannedAmountHint);

public sealed record PeriodDepreciationResult(
    decimal DepreciationAmount,
    bool IsFinalPeriod);

/// <summary>
/// Depreciation strategy. Default implementation is provisional straight-line (ADR-016).
/// </summary>
public interface IDepreciationMethod
{
    DepreciationMethodCode Method { get; }

    PeriodDepreciationResult CalculatePeriod(PeriodDepreciationRequest request);
}
