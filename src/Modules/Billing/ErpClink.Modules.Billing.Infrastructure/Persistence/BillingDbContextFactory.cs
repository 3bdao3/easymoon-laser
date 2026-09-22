using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ErpClink.Modules.Billing.Infrastructure.Persistence;

public sealed class BillingDbContextFactory : IDesignTimeDbContextFactory<BillingDbContext>
{
    public BillingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BillingDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=ErpClink_DesignTime;Trusted_Connection=True;TrustServerCertificate=True");
        return new BillingDbContext(optionsBuilder.Options);
    }
}
