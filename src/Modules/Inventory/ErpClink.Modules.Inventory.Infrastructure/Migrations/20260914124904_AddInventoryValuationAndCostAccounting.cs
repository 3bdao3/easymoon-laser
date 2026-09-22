using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryValuationAndCostAccounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InventoryValue",
                schema: "inventory",
                table: "StockBalances",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "InventoryCostLayers",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    OriginalValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCostLayers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCostTransactions",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransactionType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SourceModule = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ValuationMethod = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCostTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCostAllocations",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostLayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCostAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCostAllocations_InventoryCostTransactions_CostTransactionId",
                        column: x => x.CostTransactionId,
                        principalSchema: "inventory",
                        principalTable: "InventoryCostTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostAllocations_CostLayerId",
                schema: "inventory",
                table: "InventoryCostAllocations",
                column: "CostLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostAllocations_CostTransactionId",
                schema: "inventory",
                table: "InventoryCostAllocations",
                column: "CostTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayers_OpenFifo",
                schema: "inventory",
                table: "InventoryCostLayers",
                columns: new[] { "OrganizationId", "WarehouseId", "InventoryItemId", "Status", "ReceiptDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayers_Source",
                schema: "inventory",
                table: "InventoryCostLayers",
                columns: new[] { "OrganizationId", "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayers_StockBatchId",
                schema: "inventory",
                table: "InventoryCostLayers",
                column: "StockBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostTransactions_CorrelationId",
                schema: "inventory",
                table: "InventoryCostTransactions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostTransactions_Item_Date",
                schema: "inventory",
                table: "InventoryCostTransactions",
                columns: new[] { "OrganizationId", "InventoryItemId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostTransactions_StockMovementId",
                schema: "inventory",
                table: "InventoryCostTransactions",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "UX_InventoryCostTransactions_Org_Source_Event_Key",
                schema: "inventory",
                table: "InventoryCostTransactions",
                columns: new[] { "OrganizationId", "SourceType", "SourceId", "EventType", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryCostAllocations",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryCostLayers",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryCostTransactions",
                schema: "inventory");

            migrationBuilder.DropColumn(
                name: "InventoryValue",
                schema: "inventory",
                table: "StockBalances");
        }
    }
}
