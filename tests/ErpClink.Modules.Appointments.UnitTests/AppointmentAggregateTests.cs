using ErpClink.Modules.Appointments.Domain.Appointments;
using FluentAssertions;

namespace ErpClink.Modules.Appointments.UnitTests;

public sealed class AppointmentAggregateTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Book_creates_scheduled_appointment_with_event_and_audit()
    {
        var utc = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc);
        var appointment = Appointment.Book(
            Org, Branch, "A-2026-000001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 9, 21), new TimeOnly(9, 0), new TimeOnly(9, 30),
            "Checkup", null, "user-1", utc);

        appointment.Status.Should().Be(AppointmentStatus.Scheduled);
        appointment.AppointmentNumber.Should().Be("A-2026-000001");
        appointment.CreatedBy.Should().Be("user-1");
        appointment.DomainEvents.Should().ContainSingle(e => e is AppointmentBookedDomainEvent);
        appointment.OccupiesSlot.Should().BeTrue();
    }

    [Fact]
    public void Confirm_from_scheduled_succeeds()
    {
        var appointment = CreateBooked();
        appointment.ClearDomainEvents();
        appointment.Confirm("user-1", DateTime.UtcNow);
        appointment.Status.Should().Be(AppointmentStatus.Confirmed);
        appointment.DomainEvents.Should().ContainSingle(e => e is AppointmentConfirmedDomainEvent);
    }

    [Fact]
    public void Cancel_from_scheduled_succeeds()
    {
        var appointment = CreateBooked();
        appointment.Cancel("patient request", "user-1", DateTime.UtcNow);
        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.CancellationReason.Should().Be("patient request");
        appointment.CancelledBy.Should().Be("user-1");
        appointment.OccupiesSlot.Should().BeFalse();
    }

    [Fact]
    public void Invalid_transition_from_cancelled_rejected()
    {
        var appointment = CreateBooked();
        appointment.Cancel(null, "user-1", DateTime.UtcNow);
        var act = () => appointment.Confirm("user-1", DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NoShow_from_confirmed_succeeds()
    {
        var appointment = CreateBooked();
        appointment.Confirm("user-1", DateTime.UtcNow);
        appointment.ClearDomainEvents();
        appointment.MarkNoShow("user-1", DateTime.UtcNow);
        appointment.Status.Should().Be(AppointmentStatus.NoShow);
        appointment.DomainEvents.Should().ContainSingle(e => e is AppointmentMarkedNoShowDomainEvent);
        appointment.OccupiesSlot.Should().BeFalse();
    }

    [Fact]
    public void NoShow_from_scheduled_rejected()
    {
        var appointment = CreateBooked();
        var act = () => appointment.MarkNoShow("user-1", DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reschedule_updates_slot_and_raises_event()
    {
        var appointment = CreateBooked();
        appointment.ClearDomainEvents();
        appointment.Reschedule(new DateOnly(2026, 9, 22), new TimeOnly(10, 0), new TimeOnly(10, 30), "user-1", DateTime.UtcNow);
        appointment.AppointmentDate.Should().Be(new DateOnly(2026, 9, 22));
        appointment.StartTime.Should().Be(new TimeOnly(10, 0));
        appointment.RescheduleHistory.Should().ContainSingle();
        appointment.DomainEvents.Should().ContainSingle(e => e is AppointmentRescheduledDomainEvent);
    }

    [Fact]
    public void Invalid_time_range_on_book_rejected()
    {
        var act = () => Appointment.Book(
            Org, Branch, "A-2026-000002", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 9, 21), new TimeOnly(10, 0), new TimeOnly(9, 0),
            null, null, "user-1", DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("*StartTime*");
    }

    private static Appointment CreateBooked() =>
        Appointment.Book(
            Org, Branch, "A-2026-000010", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 9, 21), new TimeOnly(9, 0), new TimeOnly(9, 30),
            null, null, "user-1", DateTime.UtcNow);
}
