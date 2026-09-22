using ErpClink.Modules.LaserClinic.Application.Customers;
using ErpClink.Modules.LaserClinic.Domain.Appointments;
using FluentAssertions;

namespace ErpClink.Modules.LaserClinic.UnitTests;

public sealed class NextAppointmentSelectorTests
{
    private sealed record Stub(DateOnly Date, TimeOnly Start);

    [Fact]
    public void Picks_nearest_future_not_past()
    {
        var today = new DateOnly(2026, 9, 21);
        var now = new TimeOnly(12, 0);

        var past = new Stub(new DateOnly(2026, 9, 15), new TimeOnly(17, 0));
        var next = new Stub(new DateOnly(2026, 9, 25), new TimeOnly(17, 0));
        var later = new Stub(new DateOnly(2026, 10, 1), new TimeOnly(18, 0));

        var candidates = new[] { past, next, later }
            .Where(a => NextAppointmentSelector.IsUpcoming(
                a.Date,
                a.Start,
                LaserAppointmentStatus.Confirmed,
                today,
                now));

        var picked = NextAppointmentSelector.PickNext(candidates, a => a.Date, a => a.Start);
        picked.Should().Be(next);
    }

    [Fact]
    public void Ignores_cancelled_and_past_today_slot()
    {
        var today = new DateOnly(2026, 9, 22);
        var now = new TimeOnly(18, 0);

        NextAppointmentSelector.IsUpcoming(
                today,
                new TimeOnly(17, 0),
                LaserAppointmentStatus.Confirmed,
                today,
                now)
            .Should().BeFalse();

        NextAppointmentSelector.IsUpcoming(
                today,
                new TimeOnly(19, 0),
                LaserAppointmentStatus.Cancelled,
                today,
                now)
            .Should().BeFalse();

        NextAppointmentSelector.IsUpcoming(
                today,
                new TimeOnly(19, 0),
                LaserAppointmentStatus.Confirmed,
                today,
                now)
            .Should().BeTrue();
    }
}
