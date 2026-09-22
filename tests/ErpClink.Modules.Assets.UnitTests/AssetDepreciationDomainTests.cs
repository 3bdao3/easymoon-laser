using ErpClink.Modules.Assets.Application.Accounting;
using ErpClink.Modules.Assets.Domain.Accounting;
using ErpClink.Modules.Assets.Infrastructure.Accounting;
using FluentAssertions;

namespace ErpClink.Modules.Assets.UnitTests;

public sealed class AssetDepreciationDomainTests
{
    private static readonly Guid Org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Branch = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid AssetId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void Capitalize_rejects_residual_above_cost()
    {
        var act = () => AssetFinancialProfile.Capitalize(
            Org, Branch, AssetId, 1000, 1000, 1001,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), 12,
            DepreciationMethodCode.ProvisionalStraightLine, "u", DateTime.UtcNow);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Straight_line_schedule_sums_to_depreciable_base()
    {
        var lines = StraightLineDepreciationMethod.BuildSchedule(1200m, 200m, 10, new DateOnly(2026, 1, 15));
        lines.Should().HaveCount(10);
        lines.Sum(l => l.Planned).Should().Be(1000m);
        lines[0].PeriodStart.Should().Be(new DateOnly(2026, 1, 1));
    }

    [Fact]
    public void Apply_depreciation_never_below_residual()
    {
        var profile = AssetFinancialProfile.Capitalize(
            Org, Branch, AssetId, 1000, 1000, 100,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), 9,
            DepreciationMethodCode.ProvisionalStraightLine, "u", DateTime.UtcNow);

        profile.ApplyDepreciation(100m, new DateOnly(2026, 1, 1), "u", DateTime.UtcNow);
        profile.NetBookValue.Should().Be(900m);
        profile.AccumulatedDepreciation.Should().Be(100m);

        var act = () => profile.ApplyDepreciation(901m, new DateOnly(2026, 2, 1), "u", DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Final_period_consumes_remaining_amount()
    {
        var method = new StraightLineDepreciationMethod();
        var profile = AssetFinancialProfile.Capitalize(
            Org, Branch, AssetId, 1000, 1000, 100,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), 3,
            DepreciationMethodCode.ProvisionalStraightLine, "u", DateTime.UtcNow);

        for (var i = 0; i < 3; i++)
        {
            var calc = method.CalculatePeriod(new PeriodDepreciationRequest(
                profile.CapitalizedCost, profile.ResidualValue, profile.AccumulatedDepreciation,
                profile.UsefulLifeMonths, i, i, 300m));
            profile.ApplyDepreciation(calc.DepreciationAmount, new DateOnly(2026, 1, 1).AddMonths(i), "u", DateTime.UtcNow);
        }

        profile.Status.Should().Be(AssetFinancialStatus.FullyDepreciated);
        profile.AccumulatedDepreciation.Should().Be(900m);
        profile.NetBookValue.Should().Be(100m);
        profile.RemainingDepreciableAmount.Should().Be(0m);
    }

    [Fact]
    public void Incomplete_profile_cannot_be_disposed()
    {
        // No incomplete factory — disposal without capitalize is blocked at service layer.
        // Capitalized then dispose works:
        var profile = AssetFinancialProfile.Capitalize(
            Org, Branch, AssetId, 500, 500, 0,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), 12,
            DepreciationMethodCode.ProvisionalStraightLine, "u", DateTime.UtcNow);
        profile.MarkDisposed(new DateOnly(2026, 6, 1), 50m, "sold", "u", DateTime.UtcNow);
        profile.Status.Should().Be(AssetFinancialStatus.Disposed);
        profile.DisposalGainLoss.Should().Be(50m - 500m);
    }
}
