using ErpClink.Modules.MedicalVisits.Domain.Visits;
using FluentAssertions;

namespace ErpClink.Modules.MedicalVisits.UnitTests;

public sealed class MedicalVisitAggregateTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Start_creates_open_visit_with_event()
    {
        var utc = DateTime.UtcNow;
        var visit = MedicalVisit.Start(Org, Branch, "V-2026-000001", Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 14), "u1", utc);

        visit.Status.Should().Be(MedicalVisitStatus.Open);
        visit.DomainEvents.Should().ContainSingle(e => e is MedicalVisitStartedDomainEvent);
    }

    [Fact]
    public void Clinical_notes_move_open_to_inprogress()
    {
        var visit = CreateOpen();
        visit.UpdateClinicalNotes("pain", "notes", null, "dx", null, "u", DateTime.UtcNow);
        visit.Status.Should().Be(MedicalVisitStatus.InProgress);
        visit.ChiefComplaint.Should().Be("pain");
        visit.DomainEvents.Should().Contain(e => e is MedicalVisitClinicalNotesUpdatedDomainEvent);
    }

    [Fact]
    public void Complete_and_cancel_transitions()
    {
        var complete = CreateOpen();
        complete.Complete("u", DateTime.UtcNow);
        complete.Status.Should().Be(MedicalVisitStatus.Completed);
        complete.IsActive.Should().BeFalse();

        var cancel = CreateOpen();
        cancel.Cancel("wrong patient", "u", DateTime.UtcNow);
        cancel.Status.Should().Be(MedicalVisitStatus.Cancelled);
    }

    [Fact]
    public void Invalid_transitions_rejected()
    {
        var visit = CreateOpen();
        visit.Complete("u", DateTime.UtcNow);
        var act = () => visit.Cancel("x", "u", DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>();

        var notes = CreateOpen();
        notes.Complete("u", DateTime.UtcNow);
        var actNotes = () => notes.UpdateClinicalNotes("a", null, null, null, null, "u", DateTime.UtcNow);
        actNotes.Should().Throw<InvalidOperationException>();
    }

    private static MedicalVisit CreateOpen() =>
        MedicalVisit.Start(Org, Branch, "V-2026-0099", Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), null, new DateOnly(2026, 9, 14), "u1", DateTime.UtcNow);
}
