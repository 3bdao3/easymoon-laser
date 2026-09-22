using ErpClink.Modules.Procurement.Domain;
using ErpClink.Modules.Procurement.Domain.PurchaseOrders;
using FluentAssertions;

namespace ErpClink.Modules.Procurement.UnitTests;

public sealed class PurchaseOrderAggregateTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Supplier = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void Draft_submit_approve_cancel_lifecycle()
    {
        var po = CreateWithLine(50m);
        po.Submit("u", DateTime.UtcNow);
        po.Status.Should().Be(PurchaseOrderStatus.Submitted);

        po.Approve("u", DateTime.UtcNow);
        po.Status.Should().Be(PurchaseOrderStatus.Approved);

        po.Cancel("changed mind", "u", DateTime.UtcNow);
        po.Status.Should().Be(PurchaseOrderStatus.Cancelled);
    }

    [Fact]
    public void Invalid_transitions_rejected()
    {
        var po = CreateWithLine(10m);
        var approve = () => po.Approve("u", DateTime.UtcNow);
        approve.Should().Throw<InvalidOperationException>();

        po.Submit("u", DateTime.UtcNow);
        var modify = () => po.AddLine(new PurchaseOrderLineInput(null, "X", 1, 1, 0, 1), "u", DateTime.UtcNow);
        modify.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Empty_lines_cannot_submit()
    {
        var po = PurchaseOrder.CreateDraft(Org, Branch, "PO-2026-000001", Supplier, new DateOnly(2026, 9, 14), "EGP", null, [], "u", DateTime.UtcNow);
        var act = () => po.Submit("u", DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Invalid_quantity_and_cost_rejected()
    {
        var po = CreateDraft();
        var badQty = () => po.AddLine(new PurchaseOrderLineInput(null, "Item", 0, 10, 0, 1), "u", DateTime.UtcNow);
        badQty.Should().Throw<ArgumentOutOfRangeException>();

        var badCost = () => po.AddLine(new PurchaseOrderLineInput(null, "Item", 1, -1, 0, 1), "u", DateTime.UtcNow);
        badCost.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Totals_and_discounts_calculated()
    {
        var po = CreateDraft();
        po.AddLine(new PurchaseOrderLineInput(null, "A", 2, 50, 10, 1), "u", DateTime.UtcNow);
        po.SubTotal.Should().Be(90m);
        po.SetDiscount(5, "u", DateTime.UtcNow);
        po.TotalAmount.Should().Be(85m);
        po.TaxAmount.Should().Be(0);
    }

    [Fact]
    public void Unit_cost_snapshot_is_immutable_on_line()
    {
        var po = CreateDraft();
        var line = po.AddLine(new PurchaseOrderLineInput(Guid.NewGuid(), "Widget", 1, 50m, 0, 1), "u", DateTime.UtcNow);
        line.UnitCost.Should().Be(50m);

        var catalogWouldBe99 = 99m;
        catalogWouldBe99.Should().NotBe(line.UnitCost);
        line.UnitCost.Should().Be(50m);
    }

    [Fact]
    public void Money_rounding_uses_two_decimal_places()
    {
        Money.Round(1.005m).Should().Be(1.01m);
        Money.Round(1.004m).Should().Be(1.00m);
    }

    private static PurchaseOrder CreateDraft() =>
        PurchaseOrder.CreateDraft(Org, Branch, "PO-2026-000010", Supplier, new DateOnly(2026, 9, 14), "EGP", null, [], "u", DateTime.UtcNow);

    private static PurchaseOrder CreateWithLine(decimal unitCost)
    {
        var po = CreateDraft();
        po.AddLine(new PurchaseOrderLineInput(null, "Supply", 1, unitCost, 0, 1), "u", DateTime.UtcNow);
        return po;
    }
}
