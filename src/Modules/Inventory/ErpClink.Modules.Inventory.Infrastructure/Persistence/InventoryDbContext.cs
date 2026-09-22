using ErpClink.Modules.Inventory.Domain.Categories;
using ErpClink.Modules.Inventory.Domain.Costing;
using ErpClink.Modules.Inventory.Domain.GoodsReceipts;
using ErpClink.Modules.Inventory.Domain.Items;
using ErpClink.Modules.Inventory.Domain.Stock;
using ErpClink.Modules.Inventory.Domain.Warehouses;
using Microsoft.EntityFrameworkCore;

namespace ErpClink.Modules.Inventory.Infrastructure.Persistence;

public sealed class WarehouseNumberSequence
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public long LastValue { get; set; }
}

public sealed class InventoryItemNumberSequence
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public long LastValue { get; set; }
}

public sealed class GoodsReceiptNumberSequence
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public long LastValue { get; set; }
}

public sealed class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryCategory> InventoryCategories => Set<InventoryCategory>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockBatch> StockBatches => Set<StockBatch>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();
    public DbSet<InventoryCostLayer> InventoryCostLayers => Set<InventoryCostLayer>();
    public DbSet<InventoryCostTransaction> InventoryCostTransactions => Set<InventoryCostTransaction>();
    public DbSet<InventoryCostAllocation> InventoryCostAllocations => Set<InventoryCostAllocation>();
    public DbSet<WarehouseNumberSequence> WarehouseNumberSequences => Set<WarehouseNumberSequence>();
    public DbSet<InventoryItemNumberSequence> InventoryItemNumberSequences => Set<InventoryItemNumberSequence>();
    public DbSet<GoodsReceiptNumberSequence> GoodsReceiptNumberSequences => Set<GoodsReceiptNumberSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inventory");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
