using ErpClink.Modules.LaserClinic.Application.Scheduling;
using ErpClink.Modules.LaserClinic.Domain.Appointments;
using FluentAssertions;

namespace ErpClink.Modules.LaserClinic.UnitTests;

public sealed class DurationCalculatorTests
{
    [Fact]
    public void Fixed_service_uses_same_duration()
    {
        DurationCalculator.RecommendedMinutes(10, 10).Should().Be(10);
    }

    [Fact]
    public void Ranged_service_uses_max_for_booking()
    {
        DurationCalculator.RecommendedMinutes(30, 45).Should().Be(45);
    }

    [Fact]
    public void Effective_blocked_includes_buffer()
    {
        DurationCalculator.EffectiveBlockedMinutes(45, 5).Should().Be(50);
    }

    [Fact]
    public void Multiple_independent_services_sum_recommended_durations()
    {
        // Face 10 + Full Arm 25 + Half Leg 20 = 55
        var total = DurationCalculator.RecommendedMinutes(10, 10)
            + DurationCalculator.RecommendedMinutes(25, 25)
            + DurationCalculator.RecommendedMinutes(20, 20);
        total.Should().Be(55);
    }

    [Fact]
    public void Combined_service_is_single_duration_not_sum_of_parts()
    {
        DurationCalculator.RecommendedMinutes(15, 15).Should().Be(15);
    }
}

public sealed class AvailabilityAndConflictTests
{
    [Fact]
    public void Overlap_rule_detects_conflict()
    {
        LaserAppointment.Overlaps(new TimeOnly(16, 0), new TimeOnly(17, 0), new TimeOnly(16, 30), new TimeOnly(17, 30))
            .Should().BeTrue();
        LaserAppointment.Overlaps(new TimeOnly(16, 0), new TimeOnly(17, 0), new TimeOnly(17, 0), new TimeOnly(18, 0))
            .Should().BeFalse();
    }

    [Fact]
    public void Availability_finds_gap_between_bookings()
    {
        // 16:00–17:00, 17:30–18:15, 19:00–20:00
        // 45m cannot fit in the 30m gap 17:00–17:30 (would conflict with 17:30 booking).
        var occupied = new List<OccupiedInterval>
        {
            new(new TimeOnly(16, 0), new TimeOnly(17, 0), new TimeOnly(17, 0)),
            new(new TimeOnly(17, 30), new TimeOnly(18, 15), new TimeOnly(18, 15)),
            new(new TimeOnly(19, 0), new TimeOnly(20, 0), new TimeOnly(20, 0))
        };

        var slots = AvailabilityCalculator.GetAvailableSlots(
            new TimeOnly(16, 0),
            new TimeOnly(22, 0),
            15,
            clinicalDurationMinutes: 45,
            bufferMinutes: 0,
            occupied);

        slots.Should().Contain(s => s.StartTime == new TimeOnly(18, 15) && s.EndTime == new TimeOnly(19, 0));
        slots.Should().Contain(s => s.StartTime == new TimeOnly(20, 0));
        slots.Should().NotContain(s => s.StartTime == new TimeOnly(17, 0));
        slots.Should().NotContain(s => s.StartTime == new TimeOnly(16, 30));
    }

    [Fact]
    public void New_patient_style_empty_day_has_slots()
    {
        var slots = AvailabilityCalculator.GetAvailableSlots(
            new TimeOnly(16, 0),
            new TimeOnly(22, 0),
            15,
            60,
            0,
            []);

        slots.Should().NotBeEmpty();
        slots[0].StartTime.Should().Be(new TimeOnly(16, 0));
        slots[0].DurationMinutes.Should().Be(60);
    }

    [Fact]
    public void Day_timeline_marks_booked_and_unavailable_and_available()
    {
        var occupied = new List<OccupiedInterval>
        {
            new(new TimeOnly(17, 0), new TimeOnly(17, 45), new TimeOnly(17, 45),
                Guid.Parse("11111111-1111-1111-1111-111111111111"), "سارة", 45, "Full Leg"),
            new(new TimeOnly(18, 0), new TimeOnly(18, 10), new TimeOnly(18, 10),
                Guid.Parse("22222222-2222-2222-2222-222222222222"), "نور", 10, "فيس ورقبة")
        };

        var timeline = AvailabilityCalculator.GetDayTimeline(
            new TimeOnly(17, 0),
            new TimeOnly(20, 0),
            15,
            clinicalDurationMinutes: 45,
            bufferMinutes: 0,
            occupied,
            nowLocal: null,
            date: null);

        timeline.Should().Contain(s => s.StartTime == new TimeOnly(17, 0) && s.Status == DaySlotStatus.Booked && s.CustomerName == "سارة");
        timeline.Should().Contain(s => s.StartTime == new TimeOnly(17, 15) && s.Status == DaySlotStatus.Unavailable);
        timeline.Should().Contain(s => s.StartTime == new TimeOnly(17, 30) && s.Status == DaySlotStatus.Unavailable);
        timeline.Should().Contain(s => s.StartTime == new TimeOnly(17, 45) && s.Status == DaySlotStatus.Unavailable);
        timeline.Should().Contain(s => s.StartTime == new TimeOnly(18, 0) && s.Status == DaySlotStatus.Booked);
        timeline.Should().Contain(s => s.StartTime == new TimeOnly(18, 15) && s.Status == DaySlotStatus.Available);
    }
}

public sealed class CustomerDomainTests
{
    [Fact]
    public void Create_customer_requires_name_and_phone()
    {
        var act = () => Domain.Customers.Customer.Create("", "010", null, null, "u", DateTime.UtcNow);
        act.Should().Throw<ArgumentException>();
    }
}
