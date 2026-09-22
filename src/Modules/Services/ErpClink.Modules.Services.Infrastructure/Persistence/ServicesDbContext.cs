using ErpClink.Modules.Services.Domain.Catalog;
using ErpClink.Modules.Services.Domain.Categories;
using ErpClink.Modules.Services.Domain.Packages;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Services.Infrastructure.Persistence;

public sealed class ServicesDbContext : DbContext
{
    public ServicesDbContext(DbContextOptions<ServicesDbContext> options) : base(options)
    {
    }

    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<HealthcareService> HealthcareServices => Set<HealthcareService>();
    public DbSet<ServicePrice> ServicePrices => Set<ServicePrice>();
    public DbSet<HealthcarePackage> HealthcarePackages => Set<HealthcarePackage>();
    public DbSet<PackageItem> PackageItems => Set<PackageItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("services");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ServicesDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
