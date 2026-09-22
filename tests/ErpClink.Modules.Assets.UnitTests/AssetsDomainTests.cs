using ErpClink.Modules.Assets.Domain;
using ErpClink.Modules.Assets.Domain.Assets;
using ErpClink.Modules.Assets.Domain.Categories;
using FluentAssertions;

namespace ErpClink.Modules.Assets.UnitTests;

public sealed class AssetsDomainTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void AssetCategory_activate_deactivate()
    {
        var cat = AssetCategory.Create(Org, "ASC-2026-000001", "Equipment", null, "u", DateTime.UtcNow);
        cat.Deactivate("u", DateTime.UtcNow);
        cat.IsActive.Should().BeFalse();
        cat.Activate("u", DateTime.UtcNow);
        cat.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Asset_lifecycle_active_maintenance_retire()
    {
        var asset = CreateActiveAsset();
        asset.Status.Should().Be(AssetStatus.Active);

        asset.StartMaintenance("Annual service", "u", DateTime.UtcNow);
        asset.Status.Should().Be(AssetStatus.UnderMaintenance);

        asset.CompleteMaintenance("u", DateTime.UtcNow);
        asset.Status.Should().Be(AssetStatus.Active);

        asset.Retire("End of life", "u", DateTime.UtcNow);
        asset.Status.Should().Be(AssetStatus.Retired);
    }

    [Fact]
    public void Retired_asset_cannot_change_location()
    {
        var asset = CreateActiveAsset();
        asset.Retire("Disposed", "u", DateTime.UtcNow);
        var act = () => asset.ChangeLocation(Guid.NewGuid(), "u", DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Warranty_end_before_start_rejected()
    {
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = start.AddDays(-1);
        var act = () => Asset.Create(
            Org, Branch, "AST-2026-000001", "MRI", null, Guid.NewGuid(), Guid.NewGuid(), null,
            start, null, null, start, end, null, "u", DateTime.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Warranty_end_without_start_rejected()
    {
        var end = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));
        var act = () => Asset.Create(
            Org, Branch, "AST-2026-000002", "MRI", null, Guid.NewGuid(), Guid.NewGuid(), null,
            null, null, null, null, end, null, "u", DateTime.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Asset_stores_purchase_and_warranty_fields()
    {
        var purchase = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = purchase;
        var end = purchase.AddYears(2);
        var asset = Asset.Create(
            Org, Branch, "AST-2026-000003", "MRI", null, Guid.NewGuid(), Guid.NewGuid(), "SN-1",
            purchase, 150000m, "PO notes", start, end, "Vendor warranty", "u", DateTime.UtcNow);

        asset.PurchaseDate.Should().Be(purchase);
        asset.WarrantyStartDate.Should().Be(start);
        asset.WarrantyEndDate.Should().Be(end);
        asset.AcquisitionCost.Should().Be(150000m);
    }

    private static Asset CreateActiveAsset() =>
        Asset.Create(
            Org, Branch, "AST-2026-000099", "Ultrasound", null, Guid.NewGuid(), Guid.NewGuid(), null,
            DateOnly.FromDateTime(DateTime.UtcNow), null, null, null, null, null, "u", DateTime.UtcNow);
}
