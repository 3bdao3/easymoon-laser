using ErpClink.BuildingBlocks.Application.Abstractions;

namespace ErpClink.BuildingBlocks.Infrastructure.Time;

/// <summary>
/// Development default: UTC calendar date/time as business local.
/// Replace when organization timezone settings exist.
/// </summary>
public sealed class UtcBusinessClock : IBusinessClock
{
    private readonly IDateTimeProvider _clock;

    public UtcBusinessClock(IDateTimeProvider clock) => _clock = clock;

    public DateTime UtcNow => _clock.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(_clock.UtcNow);
    public TimeOnly NowLocal => TimeOnly.FromDateTime(_clock.UtcNow);
}
