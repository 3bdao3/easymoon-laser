using ErpClink.Modules.Procurement.Domain.Suppliers;
using FluentAssertions;

namespace ErpClink.Modules.Procurement.UnitTests;

public sealed class SupplierAggregateTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_activate_deactivate()
    {
        var supplier = Supplier.Create(Org, "SUP-2026-000001", "Acme Medical", null, null, null, null, null, "u", DateTime.UtcNow);
        supplier.IsActive.Should().BeTrue();

        supplier.Deactivate("u", DateTime.UtcNow);
        supplier.IsActive.Should().BeFalse();

        supplier.Activate("u", DateTime.UtcNow);
        supplier.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Update_changes_name()
    {
        var supplier = Supplier.Create(Org, "SUP-2026-000002", "Old Name", null, null, null, null, null, "u", DateTime.UtcNow);
        supplier.Update("New Name", "Contact", null, null, null, null, "u", DateTime.UtcNow);
        supplier.Name.Should().Be("New Name");
        supplier.ContactName.Should().Be("Contact");
    }
}
