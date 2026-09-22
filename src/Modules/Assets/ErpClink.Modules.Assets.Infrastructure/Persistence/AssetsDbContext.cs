using ErpClink.Modules.Assets.Domain.Accounting;
using ErpClink.Modules.Assets.Domain.Assets;
using ErpClink.Modules.Assets.Domain.Categories;
using ErpClink.Modules.Assets.Domain.History;
using ErpClink.Modules.Assets.Domain.Locations;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Assets.Infrastructure.Persistence;

public sealed class AssetNumberSequence
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public long LastValue { get; set; }
}

public sealed class AssetCategoryNumberSequence
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public long LastValue { get; set; }
}

public sealed class AssetLocationNumberSequence
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public long LastValue { get; set; }
}

public sealed class AssetsDbContext : DbContext
{
    public AssetsDbContext(DbContextOptions<AssetsDbContext> options) : base(options)
    {
    }

    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<AssetLocation> AssetLocations => Set<AssetLocation>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetHistoryEntry> AssetHistory => Set<AssetHistoryEntry>();
    public DbSet<AssetFinancialProfile> AssetFinancialProfiles => Set<AssetFinancialProfile>();
    public DbSet<AssetDepreciationScheduleLine> AssetDepreciationScheduleLines => Set<AssetDepreciationScheduleLine>();
    public DbSet<AssetDepreciationTransaction> AssetDepreciationTransactions => Set<AssetDepreciationTransaction>();
    public DbSet<AssetFinancialTransaction> AssetFinancialTransactions => Set<AssetFinancialTransaction>();
    public DbSet<AssetNumberSequence> AssetNumberSequences => Set<AssetNumberSequence>();
    public DbSet<AssetCategoryNumberSequence> AssetCategoryNumberSequences => Set<AssetCategoryNumberSequence>();
    public DbSet<AssetLocationNumberSequence> AssetLocationNumberSequences => Set<AssetLocationNumberSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("assets");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssetsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
