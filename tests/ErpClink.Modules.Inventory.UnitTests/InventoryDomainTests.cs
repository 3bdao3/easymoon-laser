using ErpClink.Modules.Inventory.Domain.GoodsReceipts;
using ErpClink.Modules.Inventory.Domain.Items;
using ErpClink.Modules.Inventory.Domain.Stock;
using ErpClink.Modules.Inventory.Domain.Warehouses;
using FluentAssertions;

namespace ErpClink.Modules.Inventory.UnitTests;

public sealed class InventoryDomainTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void StockBalance_rejects_negative_decrease()
    {
        var balance = StockBalance.Create(Org, Branch, Guid.NewGuid(), Guid.NewGuid());
        balance.Increase(5);
        var act = () => balance.Decrease(6);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Expiry_tracked_item_requires_batch_on_receive()
    {
        var item = InventoryItem.Create(Org, "ITM-1", "Med", null, null, "EA", null, true, 0, "u", DateTime.UtcNow);
        var act = () => item.EnsureBatchRequired(null, null);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GoodsReceipt_create_posted_requires_lines()
    {
        var act = () => GoodsReceipt.CreatePosted(
            Org, Branch, "GR-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow), null, [], "u", DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Warehouse_activate_deactivate()
    {
        var wh = Warehouse.Create(Org, Branch, "WH-1", "Main", null, "u", DateTime.UtcNow);
        wh.Deactivate("u", DateTime.UtcNow);
        wh.IsActive.Should().BeFalse();
        wh.Activate("u", DateTime.UtcNow);
        wh.IsActive.Should().BeTrue();
    }
}
