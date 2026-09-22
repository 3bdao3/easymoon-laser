using ErpClink.Modules.MedicalVisits.Application.Visits;
using ErpClink.Modules.MedicalVisits.Application.Visits.Models;
using ErpClink.Modules.MedicalVisits.Domain.Visits;
using FluentAssertions;

namespace ErpClink.Modules.MedicalVisits.UnitTests;

public sealed class PatientVisitContextFactoryTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Patient = Guid.NewGuid();

    [Fact]
    public void No_visits_is_new_patient_with_no_treatment()
    {
        var ctx = PatientVisitContextFactory.FromVisits([]);

        ctx.PatientRelationship.Should().Be(PatientVisitContextCodes.RelationshipNew);
        ctx.IsReturningPatient.Should().BeFalse();
        ctx.TreatmentState.Should().Be(PatientVisitContextCodes.TreatmentNone);
        ctx.CurrentEncounterKind.Should().BeNull();
        ctx.TotalVisits.Should().Be(0);
    }

    [Fact]
    public void Single_open_visit_is_new_patient_first_visit()
    {
        var visit = StartVisit("V-1", new DateOnly(2026, 9, 15));
        var ctx = PatientVisitContextFactory.FromVisits([visit]);

        ctx.PatientRelationship.Should().Be(PatientVisitContextCodes.RelationshipNew);
        ctx.CurrentEncounterKind.Should().Be(PatientVisitContextCodes.EncounterFirstVisit);
        ctx.TreatmentState.Should().Be(PatientVisitContextCodes.TreatmentInProgress);
        ctx.ActiveVisit.Should().NotBeNull();
    }

    [Fact]
    public void Completed_visit_means_returning_and_treatment_completed()
    {
        var visit = StartVisit("V-1", new DateOnly(2026, 1, 10));
        visit.Complete("dr", DateTime.UtcNow);

        var ctx = PatientVisitContextFactory.FromVisits([visit]);

        ctx.PatientRelationship.Should().Be(PatientVisitContextCodes.RelationshipReturning);
        ctx.TreatmentState.Should().Be(PatientVisitContextCodes.TreatmentCompleted);
        ctx.CurrentEncounterKind.Should().BeNull();
        ctx.IsReturningPatient.Should().BeTrue();
    }

    [Fact]
    public void Returning_patient_with_new_open_visit_is_consultation()
    {
        var previous = StartVisit("V-1", new DateOnly(2026, 1, 10));
        previous.Complete("dr", DateTime.UtcNow);

        var current = StartVisit("V-2", new DateOnly(2026, 9, 15));
        var ctx = PatientVisitContextFactory.FromVisits([previous, current]);

        ctx.PatientRelationship.Should().Be(PatientVisitContextCodes.RelationshipReturning);
        ctx.CurrentEncounterKind.Should().Be(PatientVisitContextCodes.EncounterConsultation);
        ctx.TreatmentState.Should().Be(PatientVisitContextCodes.TreatmentInProgress);
    }

    [Fact]
    public void Returning_patient_after_follow_up_notes_is_follow_up_encounter()
    {
        var previous = StartVisit("V-1", new DateOnly(2026, 1, 10));
        previous.UpdateClinicalNotes(null, null, null, null, "Return in 2 weeks", "dr", DateTime.UtcNow);
        previous.Complete("dr", DateTime.UtcNow);

        var current = StartVisit("V-2", new DateOnly(2026, 1, 24));
        var ctx = PatientVisitContextFactory.FromVisits([previous, current]);

        ctx.PatientRelationship.Should().Be(PatientVisitContextCodes.RelationshipReturning);
        ctx.CurrentEncounterKind.Should().Be(PatientVisitContextCodes.EncounterFollowUp);
        ctx.PreviousVisitHadFollowUpNotes.Should().BeTrue();
    }

    [Fact]
    public void Cancelled_visits_do_not_count_toward_relationship()
    {
        var cancelled = StartVisit("V-1", new DateOnly(2026, 1, 10));
        cancelled.Cancel("mistake", "dr", DateTime.UtcNow);

        var ctx = PatientVisitContextFactory.FromVisits([cancelled]);

        ctx.TotalVisits.Should().Be(0);
        ctx.PatientRelationship.Should().Be(PatientVisitContextCodes.RelationshipNew);
        ctx.TreatmentState.Should().Be(PatientVisitContextCodes.TreatmentNone);
    }

    private static MedicalVisit StartVisit(string number, DateOnly date) =>
        MedicalVisit.Start(
            Org,
            Branch,
            number,
            Patient,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            date,
            "tester",
            DateTime.UtcNow);
}
