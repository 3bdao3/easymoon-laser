using ErpClink.Modules.Services.Domain.Catalog;
using ErpClink.Modules.Services.Domain.Packages;
using FluentAssertions;

namespace ErpClink.Modules.Services.UnitTests;

public sealed class ServicesDomainTests
{
    private static readonly Guid OrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTime Utc = new(2026, 9, 14, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_service_opens_price_history()
    {
        var service = HealthcareService.Create(OrgId, "SVC-01", "Consultation", null, null, 150m, "EGP", 30, "user", Utc);
        service.DefaultPrice.Should().Be(150m);
        service.Prices.Should().HaveCount(1);
        service.Prices.Single().EffectiveToUtc.Should().BeNull();
        service.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Update_price_closes_previous_row()
    {
        var service = HealthcareService.Create(OrgId, "SVC-02", "X-Ray", null, null, 100m, "EGP", null, "user", Utc);
        service.Update("X-Ray", null, null, 120m, null, null, "user", Utc.AddHours(1));
        service.DefaultPrice.Should().Be(120m);
        service.Prices.Should().HaveCount(2);
        service.Prices.Count(p => p.EffectiveToUtc is null).Should().Be(1);
        service.Prices.Count(p => p.EffectiveToUtc is not null).Should().Be(1);
    }

    [Fact]
    public void Package_rejects_duplicate_service_item()
    {
        var serviceId = Guid.NewGuid();
        var package = HealthcarePackage.Create(OrgId, "PKG-01", "Checkup", null, "user", Utc);
        package.AddItem(serviceId, 1, 1, "user", Utc);
        var act = () => package.AddItem(serviceId, 2, 2, "user", Utc);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Inactive_package_cannot_add_items()
    {
        var package = HealthcarePackage.Create(OrgId, "PKG-02", "Bundle", null, "user", Utc);
        package.Deactivate("user", Utc);
        var act = () => package.AddItem(Guid.NewGuid(), 1, 1, "user", Utc);
        act.Should().Throw<InvalidOperationException>();
    }
}
