using ErpClink.Modules.Assets.Application.Accounting;
using ErpClink.Modules.Assets.Domain;
using ErpClink.Modules.Assets.Domain.Accounting;

namespace ErpClink.Modules.Assets.Infrastructure.Accounting;

/// <summary>Provisional straight-line monthly depreciation. Not a finalized product decision — ADR-016.</summary>
public sealed class StraightLineDepreciationMethod : IDepreciationMethod
{
    public DepreciationMethodCode Method => DepreciationMethodCode.ProvisionalStraightLine;

    public PeriodDepreciationResult CalculatePeriod(PeriodDepreciationRequest request)
    {
        Money.EnsureNonNegative(request.CapitalizedCost, nameof(request.CapitalizedCost));
        Money.EnsureNonNegative(request.ResidualValue, nameof(request.ResidualValue));
        Money.EnsureNonNegative(request.AccumulatedDepreciation, nameof(request.AccumulatedDepreciation));
        if (request.UsefulLifeMonths <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.UsefulLifeMonths));

        var depreciableBase = Money.Round(request.CapitalizedCost - request.ResidualValue);
        var remaining = Money.Round(depreciableBase - request.AccumulatedDepreciation);
        if (remaining <= 0)
            return new PeriodDepreciationResult(0m, true);

        var periodsLeftIncludingCurrent = request.UsefulLifeMonths - request.PostedPeriodCount;
        if (periodsLeftIncludingCurrent <= 0)
            return new PeriodDepreciationResult(remaining, true);

        var isFinal = periodsLeftIncludingCurrent == 1 || remaining <= request.PlannedAmountHint;
        if (isFinal || periodsLeftIncludingCurrent == 1)
            return new PeriodDepreciationResult(remaining, true);

        var standard = Money.Round(depreciableBase / request.UsefulLifeMonths);
        var amount = Math.Min(standard, remaining);
        amount = Money.Round(amount);
        if (Money.Round(remaining - amount) < 0.01m && periodsLeftIncludingCurrent <= 2)
            return new PeriodDepreciationResult(remaining, true);

        return new PeriodDepreciationResult(amount, false);
    }

    public static IReadOnlyList<(DateOnly PeriodStart, decimal Planned)> BuildSchedule(
        decimal capitalizedCost,
        decimal residualValue,
        int usefulLifeMonths,
        DateOnly depreciationStartDate)
    {
        var baseAmount = Money.Round(capitalizedCost - residualValue);
        if (usefulLifeMonths <= 0 || baseAmount <= 0)
            return [];

        var monthly = Money.Round(baseAmount / usefulLifeMonths);
        var lines = new List<(DateOnly, decimal)>(usefulLifeMonths);
        var allocated = 0m;
        var cursor = new DateOnly(depreciationStartDate.Year, depreciationStartDate.Month, 1);

        for (var i = 0; i < usefulLifeMonths; i++)
        {
            var amount = i == usefulLifeMonths - 1
                ? Money.Round(baseAmount - allocated)
                : monthly;
            allocated = Money.Round(allocated + amount);
            lines.Add((cursor, amount));
            cursor = cursor.AddMonths(1);
        }

        return lines;
    }
}
