using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ErpClink.Modules.Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=ErpClink_DesignTime;Trusted_Connection=True;TrustServerCertificate=True");
        return new InventoryDbContext(optionsBuilder.Options);
    }
}
