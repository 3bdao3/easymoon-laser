using ErpClink.Modules.Doctors.Domain.Clinics;
using ErpClink.Modules.Doctors.Domain.Doctors;
using FluentAssertions;

namespace ErpClink.Modules.Doctors.UnitTests;

public sealed class DoctorAggregateTests
{
    private static readonly Guid OrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BranchId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Register_raises_DoctorRegistered_event_and_sets_audit()
    {
        var utc = new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);
        var doctor = Doctor.Register(
            OrgId, BranchId, "D-2026-000001", null,
            "Sara", "Ali", null, null, null, "+966500000001", "sara@example.com",
            "user-1", utc);

        doctor.DoctorNumber.Should().Be("D-2026-000001");
        doctor.CreatedBy.Should().Be("user-1");
        doctor.CreatedAtUtc.Should().Be(utc);
        doctor.DomainEvents.Should().ContainSingle(e => e is DoctorRegisteredDomainEvent);
    }

    [Fact]
    public void Inactive_doctor_cannot_be_updated()
    {
        var utc = DateTime.UtcNow;
        var doctor = Doctor.Register(OrgId, BranchId, "D-2026-000002", null,
            "Omar", "Hassan", null, null, null, null, null, "user-1", utc);
        doctor.Deactivate("user-1", utc);

        var act = () => doctor.Update("Omar", "Hassan", null, null, null, null, null, null, "user-1", utc);
        act.Should().Throw<InvalidOperationException>().WithMessage("*inactive*");
    }

    [Fact]
    public void Activate_and_deactivate_raise_events()
    {
        var utc = DateTime.UtcNow;
        var doctor = Doctor.Register(OrgId, BranchId, "D-2026-000003", null,
            "Lina", "Nasser", null, null, null, null, null, "user-1", utc);
        doctor.ClearDomainEvents();

        doctor.Deactivate("user-1", utc);
        doctor.DomainEvents.Should().ContainSingle(e => e is DoctorDeactivatedDomainEvent);
        doctor.ClearDomainEvents();

        doctor.Activate("user-1", utc);
        doctor.DomainEvents.Should().ContainSingle(e => e is DoctorActivatedDomainEvent);
        doctor.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Assignment_rejects_end_date_before_start()
    {
        var utc = DateTime.UtcNow;
        var act = () => DoctorClinicAssignment.Create(
            OrgId, BranchId, Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 1),
            "user-1", utc);

        act.Should().Throw<InvalidOperationException>().WithMessage("*EndDate*");
    }

    [Fact]
    public void Clinic_create_raises_ClinicCreated_event()
    {
        var utc = DateTime.UtcNow;
        var clinic = Clinic.Create(OrgId, BranchId, "CARD", "Cardiology Clinic", null, "Room 1", "user-1", utc);
        clinic.Code.Should().Be("CARD");
        clinic.DomainEvents.Should().ContainSingle(e => e is ClinicCreatedDomainEvent);
    }
}
