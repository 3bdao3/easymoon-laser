using ErpClink.Modules.Prescriptions.Domain.Medications;
using ErpClink.Modules.Prescriptions.Domain.Prescriptions;
using FluentAssertions;

namespace ErpClink.Modules.Prescriptions.UnitTests;

public sealed class MedicationAggregateTests
{
    [Fact]
    public void Create_activate_deactivate()
    {
        var med = Medication.Create(Guid.NewGuid(), "PARA500", "Paracetamol", "Acetaminophen",
            "500mg", "Tablet", "Oral", "u", DateTime.UtcNow);
        med.IsActive.Should().BeTrue();
        med.Deactivate("u", DateTime.UtcNow);
        med.IsActive.Should().BeFalse();
        med.Activate("u", DateTime.UtcNow);
        med.IsActive.Should().BeTrue();
    }
}

public sealed class PrescriptionAggregateTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Draft_items_issue_and_immutability()
    {
        var rx = CreateDraft();
        rx.AddItem(Guid.NewGuid(), "Paracetamol 500mg", "500mg", "TID", "5 days", "Oral", null, 10, null, null, "u", DateTime.UtcNow);
        rx.Issue("u", DateTime.UtcNow);
        rx.Status.Should().Be(PrescriptionStatus.Issued);

        var act = () => rx.AddItem(Guid.NewGuid(), "X", "1", "OD", null, null, null, null, null, null, "u", DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cannot_issue_empty_and_cancel_works()
    {
        var empty = CreateDraft();
        var act = () => empty.Issue("u", DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>();

        var rx = CreateDraft();
        rx.AddItem(Guid.NewGuid(), "Med", "1mg", "BID", null, null, null, null, null, null, "u", DateTime.UtcNow);
        rx.Cancel("wrong", "u", DateTime.UtcNow);
        rx.Status.Should().Be(PrescriptionStatus.Cancelled);
    }

    private static Prescription CreateDraft() =>
        Prescription.CreateDraft(Org, Branch, "RX-2026-000001", Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), new DateOnly(2026, 9, 14), null, "u", DateTime.UtcNow);
}
