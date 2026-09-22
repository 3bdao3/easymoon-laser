using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ErpClink.Modules.Procurement.Infrastructure.Persistence;

public sealed class ProcurementDbContextFactory : IDesignTimeDbContextFactory<ProcurementDbContext>
{
    public ProcurementDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProcurementDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=ErpClink_DesignTime;Trusted_Connection=True;TrustServerCertificate=True");
        return new ProcurementDbContext(optionsBuilder.Options);
    }
}
