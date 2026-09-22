using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.Assets.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "assets");

            migrationBuilder.CreateTable(
                name: "AssetCategories",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetCategoryNumberSequences",
                schema: "assets",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetCategoryNumberSequences", x => new { x.OrganizationId, x.Year });
                });

            migrationBuilder.CreateTable(
                name: "AssetHistory",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OccurredBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetLocationNumberSequences",
                schema: "assets",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetLocationNumberSequences", x => new { x.OrganizationId, x.Year });
                });

            migrationBuilder.CreateTable(
                name: "AssetLocations",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetNumberSequences",
                schema: "assets",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetNumberSequences", x => new { x.OrganizationId, x.Year });
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AssetCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AcquisitionCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AcquisitionReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WarrantyExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    WarrantyNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MaintenanceNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RetiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetiredBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RetirementReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetCategories_OrganizationId_Code",
                schema: "assets",
                table: "AssetCategories",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetHistory_AssetId_OccurredAtUtc",
                schema: "assets",
                table: "AssetHistory",
                columns: new[] { "AssetId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetHistory_OrganizationId",
                schema: "assets",
                table: "AssetHistory",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetLocations_OrganizationId_BranchId_IsActive",
                schema: "assets",
                table: "AssetLocations",
                columns: new[] { "OrganizationId", "BranchId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetLocations_OrganizationId_Code",
                schema: "assets",
                table: "AssetLocations",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetCategoryId",
                schema: "assets",
                table: "Assets",
                column: "AssetCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetLocationId",
                schema: "assets",
                table: "Assets",
                column: "AssetLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_OrganizationId_AssetNumber",
                schema: "assets",
                table: "Assets",
                columns: new[] { "OrganizationId", "AssetNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_OrganizationId_BranchId_Status",
                schema: "assets",
                table: "Assets",
                columns: new[] { "OrganizationId", "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_OrganizationId_SerialNumber",
                schema: "assets",
                table: "Assets",
                columns: new[] { "OrganizationId", "SerialNumber" },
                unique: true,
                filter: "[SerialNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetCategories",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "AssetCategoryNumberSequences",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "AssetHistory",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "AssetLocationNumberSequences",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "AssetLocations",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "AssetNumberSequences",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "Assets",
                schema: "assets");
        }
    }
}
