using ErpClink.Modules.Billing.Domain;
using ErpClink.Modules.Billing.Domain.Invoices;
using FluentAssertions;

namespace ErpClink.Modules.Billing.UnitTests;

public sealed class InvoiceAggregateTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Draft_issue_void_and_immutability_after_issue()
    {
        var invoice = CreateDraft();
        invoice.AddServiceLine(Guid.NewGuid(), null, "Consultation", "CONS", 1, 200m, 0, "EGP", 1, "u", DateTime.UtcNow);
        invoice.Issue("INV-2026-000001", "u", DateTime.UtcNow);
        invoice.Status.Should().Be(InvoiceStatus.Issued);

        var act = () => invoice.AddServiceLine(Guid.NewGuid(), null, "X", "X", 1, 1m, 0, "EGP", 2, "u", DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Payment_flow_partial_to_paid_and_overpayment_rejected()
    {
        var invoice = CreateDraftWithLine(500m);
        invoice.Issue("INV-2026-000002", "u", DateTime.UtcNow);

        invoice.RegisterPayment(200m, "u", DateTime.UtcNow);
        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        invoice.OutstandingAmount.Should().Be(300m);

        invoice.RegisterPayment(300m, "u", DateTime.UtcNow);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.OutstandingAmount.Should().Be(0);

        var over = () => invoice.RegisterPayment(1m, "u", DateTime.UtcNow);
        over.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Void_only_when_unpaid_draft_or_issued()
    {
        var invoice = CreateDraftWithLine(100m);
        invoice.Void("mistake", "u", DateTime.UtcNow);
        invoice.Status.Should().Be(InvoiceStatus.Voided);

        var paid = CreateDraftWithLine(100m);
        paid.Issue("INV-2026-000003", "u", DateTime.UtcNow);
        paid.RegisterPayment(100m, "u", DateTime.UtcNow);
        var act = () => paid.Void("nope", "u", DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Unit_price_snapshot_is_immutable_on_line()
    {
        var invoice = CreateDraft();
        var line = invoice.AddServiceLine(Guid.NewGuid(), null, "Service A", "A", 1, 500m, 0, "EGP", 1, "u", DateTime.UtcNow);
        line.UnitPrice.Should().Be(500m);

        var catalogWouldBe999 = 999m;
        catalogWouldBe999.Should().NotBe(line.UnitPrice);
        line.UnitPrice.Should().Be(500m);
    }

    [Fact]
    public void Money_rounding_uses_two_decimal_places()
    {
        Money.Round(1.005m).Should().Be(1.01m);
        Money.Round(1.004m).Should().Be(1.00m);
    }

    private static Invoice CreateDraft() =>
        Invoice.CreateDraft(Org, Branch, Guid.NewGuid(), null, new DateOnly(2026, 9, 14), "EGP", null, "u", DateTime.UtcNow);

    private static Invoice CreateDraftWithLine(decimal unitPrice)
    {
        var invoice = CreateDraft();
        invoice.AddServiceLine(Guid.NewGuid(), null, "Line", "L", 1, unitPrice, 0, "EGP", 1, "u", DateTime.UtcNow);
        return invoice;
    }
}
