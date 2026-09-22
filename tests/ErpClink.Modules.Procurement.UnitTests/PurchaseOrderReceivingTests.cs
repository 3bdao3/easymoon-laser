using ErpClink.Modules.Procurement.Domain.PurchaseOrders;
using FluentAssertions;

namespace ErpClink.Modules.Procurement.UnitTests;

public sealed class PurchaseOrderReceivingTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Supplier = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void ApplyReceipt_partial_then_received()
    {
        var po = CreateApproved(10);
        po.ApplyReceipt([(po.Lines.Single().Id, 4)], "u", DateTime.UtcNow);
        po.Status.Should().Be(PurchaseOrderStatus.PartiallyReceived);
        po.Lines.Single().QuantityReceived.Should().Be(4);

        po.ApplyReceipt([(po.Lines.Single().Id, 6)], "u", DateTime.UtcNow);
        po.Status.Should().Be(PurchaseOrderStatus.Received);
        po.Lines.Single().QuantityReceived.Should().Be(10);
    }

    [Fact]
    public void ApplyReceipt_rejects_over_receive()
    {
        var po = CreateApproved(5);
        var act = () => po.ApplyReceipt([(po.Lines.Single().Id, 6)], "u", DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>();
    }

    private static PurchaseOrder CreateApproved(decimal qty)
    {
        var po = PurchaseOrder.CreateDraft(Org, Branch, "PO-1", Supplier, DateOnly.FromDateTime(DateTime.UtcNow), "EGP", null, [], "u", DateTime.UtcNow);
        var line = po.AddLine(new PurchaseOrderLineInput(null, "Item", qty, 10, 0, 1), "u", DateTime.UtcNow);
        po.Submit("u", DateTime.UtcNow);
        po.Approve("u", DateTime.UtcNow);
        line.Should().NotBeNull();
        return po;
    }
}
