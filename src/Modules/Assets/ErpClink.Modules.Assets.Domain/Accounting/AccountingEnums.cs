namespace ErpClink.Modules.Assets.Domain.Accounting;

/// <summary>Financial lifecycle — separate from operational <see cref="AssetStatus"/>.</summary>
public enum AssetFinancialStatus
{
    Incomplete = 0,
    Capitalized = 1,
    Depreciating = 2,
    FullyDepreciated = 3,
    Disposed = 4
}

/// <summary>Provisional until product approves a method (ADR-016).</summary>
public enum DepreciationMethodCode
{
    ProvisionalStraightLine = 0
}

public enum DepreciationScheduleLineStatus
{
    Planned = 0,
    Posted = 1,
    Skipped = 2
}

public enum AssetFinancialEventType
{
    Capitalization = 0,
    Depreciation = 1,
    Disposal = 2,
    Correction = 3
}
