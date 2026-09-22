using ErpClink.Modules.Inventory.Domain.Costing;
using FluentAssertions;

namespace ErpClink.Modules.Inventory.UnitTests;

public sealed class InventoryCostLayerFifoTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTime Now = DateTime.UtcNow;

    [Fact]
    public void Fifo_partial_consume_and_remaining_value()
    {
        var layer = InventoryCostLayer.Create(
            Org, Branch, Guid.NewGuid(), Guid.NewGuid(), null, "GoodsReceipt", Guid.NewGuid(),
            new DateOnly(2026, 1, 1), 10m, 100m, Now);

        var consumed = layer.Consume(4m);
        consumed.Should().Be(400m);
        layer.RemainingQuantity.Should().Be(6m);
        layer.RemainingValue.Should().Be(600m);
        layer.Status.Should().Be(InventoryCostLayerStatus.Open);
    }

    [Fact]
    public void Fully_consumed_layer_clears_remaining_value()
    {
        var layer = InventoryCostLayer.Create(
            Org, Branch, Guid.NewGuid(), Guid.NewGuid(), null, "GoodsReceipt", Guid.NewGuid(),
            new DateOnly(2026, 1, 1), 3m, 10.333333m, Now);

        var v1 = layer.Consume(1m);
        var v2 = layer.Consume(1m);
        var v3 = layer.Consume(1m);
        (v1 + v2 + v3).Should().Be(layer.OriginalValue);
        layer.RemainingQuantity.Should().Be(0);
        layer.RemainingValue.Should().Be(0);
        layer.Status.Should().Be(InventoryCostLayerStatus.FullyConsumed);
    }

    [Fact]
    public void Cannot_over_consume_layer()
    {
        var layer = InventoryCostLayer.Create(
            Org, Branch, Guid.NewGuid(), Guid.NewGuid(), null, "GoodsReceipt", Guid.NewGuid(),
            new DateOnly(2026, 1, 1), 2m, 50m, Now);
        var act = () => layer.Consume(3m);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reverse_requires_unconsumed_layer()
    {
        var layer = InventoryCostLayer.Create(
            Org, Branch, Guid.NewGuid(), Guid.NewGuid(), null, "GoodsReceipt", Guid.NewGuid(),
            new DateOnly(2026, 1, 1), 5m, 20m, Now);
        layer.Consume(1m);
        var act = () => layer.MarkReversed();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Negative_unit_cost_rejected()
    {
        var act = () => InventoryCostLayer.Create(
            Org, Branch, Guid.NewGuid(), Guid.NewGuid(), null, "GoodsReceipt", Guid.NewGuid(),
            new DateOnly(2026, 1, 1), 1m, -1m, Now);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
