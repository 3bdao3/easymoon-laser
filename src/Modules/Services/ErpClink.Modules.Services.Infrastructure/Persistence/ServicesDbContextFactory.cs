using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ErpClink.Modules.Services.Infrastructure.Persistence;

public sealed class ServicesDbContextFactory : IDesignTimeDbContextFactory<ServicesDbContext>
{
    public ServicesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ServicesDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=ErpClink_DesignTime;Trusted_Connection=True;TrustServerCertificate=True");
        return new ServicesDbContext(optionsBuilder.Options);
    }
}
