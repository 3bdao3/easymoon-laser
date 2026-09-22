using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.Assets.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetAccountingAndDepreciation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetDepreciationScheduleLines",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodIndex = table.Column<int>(type: "int", nullable: false),
                    PeriodKey = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    PeriodStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PlannedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PostedTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetDepreciationScheduleLines", x => x.Id);
                    table.CheckConstraint("CK_AssetDepreciationSchedule_Planned", "[PlannedAmount] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "AssetDepreciationTransactions",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PeriodKey = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    PeriodStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OpeningNetBookValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepreciationAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingAccumulatedDepreciation = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingNetBookValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    CalculationVersion = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetDepreciationTransactions", x => x.Id);
                    table.CheckConstraint("CK_AssetDepreciationTransactions_Amounts", "[DepreciationAmount] > 0 AND [OpeningNetBookValue] >= 0 AND [ClosingNetBookValue] >= 0 AND [ClosingAccumulatedDepreciation] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "AssetFinancialProfiles",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AcquisitionCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CapitalizedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ResidualValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CapitalizationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DepreciationStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UsefulLifeMonths = table.Column<int>(type: "int", nullable: false),
                    DepreciationMethod = table.Column<int>(type: "int", nullable: false),
                    CalculationVersion = table.Column<int>(type: "int", nullable: false),
                    AccumulatedDepreciation = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetBookValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LastDepreciationPeriod = table.Column<DateOnly>(type: "date", nullable: true),
                    DisposedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DisposalProceeds = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DisposalGainLoss = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DisposalNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetFinancialProfiles", x => x.Id);
                    table.CheckConstraint("CK_AssetFinancialProfiles_Accum", "[AccumulatedDepreciation] >= 0 AND [NetBookValue] >= 0 AND [UsefulLifeMonths] > 0");
                    table.CheckConstraint("CK_AssetFinancialProfiles_Costs", "[AcquisitionCost] >= 0 AND [CapitalizedCost] >= 0 AND [ResidualValue] >= 0 AND [ResidualValue] <= [CapitalizedCost]");
                });

            migrationBuilder.CreateTable(
                name: "AssetFinancialTransactions",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OpeningNetBookValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ClosingNetBookValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AccumulatedDepreciation = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetFinancialTransactions", x => x.Id);
                    table.CheckConstraint("CK_AssetFinancialTransactions_Amount", "[Amount] >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetDepreciationSchedule_Open",
                schema: "assets",
                table: "AssetDepreciationScheduleLines",
                columns: new[] { "OrganizationId", "AssetId", "Status", "PeriodIndex" });

            migrationBuilder.CreateIndex(
                name: "UX_AssetDepreciationSchedule_Org_Asset_Period",
                schema: "assets",
                table: "AssetDepreciationScheduleLines",
                columns: new[] { "OrganizationId", "AssetId", "PeriodKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetDepreciationTransactions_Asset_Period",
                schema: "assets",
                table: "AssetDepreciationTransactions",
                columns: new[] { "OrganizationId", "AssetId", "PeriodStartDate" });

            migrationBuilder.CreateIndex(
                name: "UX_AssetDepreciationTransactions_Idempotency",
                schema: "assets",
                table: "AssetDepreciationTransactions",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AssetDepreciationTransactions_Org_Asset_Period",
                schema: "assets",
                table: "AssetDepreciationTransactions",
                columns: new[] { "OrganizationId", "AssetId", "PeriodKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetFinancialProfiles_Org_Branch_Status",
                schema: "assets",
                table: "AssetFinancialProfiles",
                columns: new[] { "OrganizationId", "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_AssetFinancialProfiles_Org_Asset",
                schema: "assets",
                table: "AssetFinancialProfiles",
                columns: new[] { "OrganizationId", "AssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetFinancialTransactions_Asset_Date",
                schema: "assets",
                table: "AssetFinancialTransactions",
                columns: new[] { "OrganizationId", "AssetId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "UX_AssetFinancialTransactions_Idempotency",
                schema: "assets",
                table: "AssetFinancialTransactions",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetDepreciationScheduleLines",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "AssetDepreciationTransactions",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "AssetFinancialProfiles",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "AssetFinancialTransactions",
                schema: "assets");
        }
    }
}
