using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ErpClink.Modules.Assets.Infrastructure.Persistence;

public sealed class AssetsDbContextFactory : IDesignTimeDbContextFactory<AssetsDbContext>
{
    public AssetsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AssetsDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=ErpClink_DesignTime;Trusted_Connection=True;TrustServerCertificate=True");
        return new AssetsDbContext(optionsBuilder.Options);
    }
}
