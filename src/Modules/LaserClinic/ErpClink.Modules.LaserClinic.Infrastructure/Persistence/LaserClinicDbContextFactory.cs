using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ErpClink.Modules.LaserClinic.Infrastructure.Persistence;

public sealed class LaserClinicDbContextFactory : IDesignTimeDbContextFactory<LaserClinicDbContext>
{
    public LaserClinicDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LaserClinicDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ErpClink;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new LaserClinicDbContext(options);
    }
}
