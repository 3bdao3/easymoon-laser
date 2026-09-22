using ErpClink.Modules.Patients.Domain.Patients;
using FluentAssertions;

namespace ErpClink.Modules.Patients.UnitTests;

public sealed class PatientAggregateTests
{
    [Fact]
    public void Register_raises_PatientRegistered_event_and_generates_audit()
    {
        var utc = new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);
        var patient = Patient.Register(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "P-2026-000001",
            "Sara",
            null,
            "Ali",
            new DateOnly(1990, 1, 1),
            Gender.Female,
            null,
            "+966500000001",
            "sara@example.com",
            null,
            null,
            null,
            null,
            null,
            "user-1",
            utc);

        patient.PatientNumber.Should().Be("P-2026-000001");
        patient.CreatedBy.Should().Be("user-1");
        patient.CreatedAtUtc.Should().Be(utc);
        patient.DomainEvents.Should().ContainSingle(e => e is PatientRegisteredDomainEvent);
        var domainEvent = patient.DomainEvents.OfType<PatientRegisteredDomainEvent>().Single();
        domainEvent.PatientId.Should().Be(patient.Id);
        domainEvent.PatientNumber.Should().Be("P-2026-000001");
    }

    [Fact]
    public void Inactive_patient_cannot_be_updated()
    {
        var utc = DateTime.UtcNow;
        var patient = Patient.Register(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "P-2026-000002",
            "Omar",
            null,
            "Hassan",
            new DateOnly(1985, 5, 5),
            Gender.Male,
            null,
            "+966500000002",
            null,
            null,
            null,
            null,
            null,
            null,
            "user-1",
            utc);

        patient.Deactivate("user-1", utc);

        var act = () => patient.UpdateDemographics(
            "Omar",
            null,
            "Hassan",
            new DateOnly(1985, 5, 5),
            Gender.Male,
            null,
            "+966500000002",
            null,
            null,
            null,
            null,
            null,
            null,
            "user-1",
            utc);

        act.Should().Throw<InvalidOperationException>().WithMessage("*inactive*");
    }

    [Fact]
    public void Allergy_can_be_added_updated_and_deactivated()
    {
        var utc = DateTime.UtcNow;
        var patient = Patient.Register(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "P-2026-000003",
            "Lina",
            null,
            "Nasser",
            new DateOnly(1992, 3, 3),
            Gender.Female,
            null,
            "+966500000003",
            null,
            null,
            null,
            null,
            null,
            null,
            "user-1",
            utc);

        var allergy = patient.AddAllergy("Penicillin", "Rash", AllergySeverity.Moderate, "Noted at registration", "user-1", utc);
        allergy.IsActive.Should().BeTrue();

        patient.UpdateAllergy(allergy.Id, "Penicillin", "Anaphylaxis", AllergySeverity.Severe, "Updated", "user-1", utc);
        allergy.Severity.Should().Be(AllergySeverity.Severe);

        patient.DeactivateAllergy(allergy.Id, "user-1", utc);
        allergy.IsActive.Should().BeFalse();
    }
}
