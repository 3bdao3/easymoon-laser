using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ErpClink.Modules.Finance.Infrastructure.Persistence;

public sealed class FinanceDbContextFactory : IDesignTimeDbContextFactory<FinanceDbContext>
{
    public FinanceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ErpClink_FinanceDesign;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new FinanceDbContext(options);
    }
}
