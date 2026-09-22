using ErpClink.Modules.Scheduling.Domain.Availability;
using ErpClink.Modules.Scheduling.Domain.Schedules;
using FluentAssertions;

namespace ErpClink.Modules.Scheduling.UnitTests;

public sealed class SlotGeneratorTests
{
    [Fact]
    public void Generates_full_slots_for_four_hour_window()
    {
        var slots = SlotGenerator.Generate(new TimeOnly(9, 0), new TimeOnly(13, 0), 30);
        slots.Select(s => s.Start).Should().Equal(
            new TimeOnly(9, 0), new TimeOnly(9, 30), new TimeOnly(10, 0), new TimeOnly(10, 30),
            new TimeOnly(11, 0), new TimeOnly(11, 30), new TimeOnly(12, 0), new TimeOnly(12, 30));
        slots.Should().OnlyContain(s => s.End - s.Start == TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void Excludes_incomplete_final_slot()
    {
        var slots = SlotGenerator.Generate(new TimeOnly(9, 0), new TimeOnly(10, 15), 30);
        slots.Select(s => s.Start).Should().Equal(new TimeOnly(9, 0), new TimeOnly(9, 30));
        slots.Should().NotContain(s => s.Start == new TimeOnly(10, 0));
    }

    [Fact]
    public void Multiple_periods_are_union_of_generated_slots()
    {
        var morning = SlotGenerator.Generate(new TimeOnly(9, 0), new TimeOnly(11, 0), 30);
        var afternoon = SlotGenerator.Generate(new TimeOnly(14, 0), new TimeOnly(16, 0), 30);
        var all = morning.Concat(afternoon).Select(s => s.Start).ToList();
        all.Should().Equal(
            new TimeOnly(9, 0), new TimeOnly(9, 30), new TimeOnly(10, 0), new TimeOnly(10, 30),
            new TimeOnly(14, 0), new TimeOnly(14, 30), new TimeOnly(15, 0), new TimeOnly(15, 30));
    }
}

public sealed class DoctorWorkingScheduleTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Invalid_time_range_rejected()
    {
        var act = () => DoctorWorkingSchedule.Create(
            Org, Branch, Guid.NewGuid(), Guid.NewGuid(), DayOfWeek.Monday,
            new TimeOnly(13, 0), new TimeOnly(9, 0), 30,
            new DateOnly(2026, 1, 1), null, "u", DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("*StartTime*");
    }

    [Fact]
    public void Invalid_slot_duration_rejected()
    {
        var act = () => DoctorWorkingSchedule.Create(
            Org, Branch, Guid.NewGuid(), Guid.NewGuid(), DayOfWeek.Monday,
            new TimeOnly(9, 0), new TimeOnly(13, 0), 0,
            new DateOnly(2026, 1, 1), null, "u", DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("*SlotDuration*");
    }

    [Fact]
    public void Overlapping_same_day_periods_detected()
    {
        var a = DoctorWorkingSchedule.Create(Org, Branch, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), DayOfWeek.Monday,
            new TimeOnly(9, 0), new TimeOnly(13, 0), 30, new DateOnly(2026, 1, 1), null, "u", DateTime.UtcNow);
        var b = DoctorWorkingSchedule.Create(Org, Branch, a.DoctorId, a.ClinicId, DayOfWeek.Monday,
            new TimeOnly(12, 0), new TimeOnly(15, 0), 30, new DateOnly(2026, 1, 1), null, "u", DateTime.UtcNow);
        a.OverlapsWith(b).Should().BeTrue();
    }

    [Fact]
    public void Non_overlapping_same_day_periods_allowed()
    {
        var a = DoctorWorkingSchedule.Create(Org, Branch, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), DayOfWeek.Monday,
            new TimeOnly(9, 0), new TimeOnly(13, 0), 30, new DateOnly(2026, 1, 1), null, "u", DateTime.UtcNow);
        var b = DoctorWorkingSchedule.Create(Org, Branch, a.DoctorId, a.ClinicId, DayOfWeek.Monday,
            new TimeOnly(17, 0), new TimeOnly(21, 0), 30, new DateOnly(2026, 1, 1), null, "u", DateTime.UtcNow);
        a.OverlapsWith(b).Should().BeFalse();
    }

    [Fact]
    public void Create_raises_domain_event_and_sets_audit()
    {
        var utc = new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);
        var schedule = DoctorWorkingSchedule.Create(
            Org, Branch, Guid.NewGuid(), Guid.NewGuid(), DayOfWeek.Tuesday,
            new TimeOnly(9, 0), new TimeOnly(12, 0), 30,
            new DateOnly(2026, 1, 1), null, "user-1", utc);
        schedule.CreatedBy.Should().Be("user-1");
        schedule.CreatedAtUtc.Should().Be(utc);
        schedule.DomainEvents.Should().ContainSingle(e => e is DoctorScheduleCreatedDomainEvent);
    }
}
