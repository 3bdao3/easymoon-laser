namespace ErpClink.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Business-local clock for date/time comparisons (past appointments, same-day rules).
/// Org timezone is unresolved; current default treats UTC as the clinic business clock.
/// </summary>
public interface IBusinessClock
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
    TimeOnly NowLocal { get; }
}
