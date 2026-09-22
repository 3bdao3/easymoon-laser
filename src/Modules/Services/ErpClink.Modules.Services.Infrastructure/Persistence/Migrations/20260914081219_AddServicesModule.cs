using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.Services.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServicesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "services");

            migrationBuilder.CreateTable(
                name: "HealthcarePackages",
                schema: "services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthcarePackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HealthcareServices",
                schema: "services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefaultPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthcareServices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCategories",
                schema: "services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PackageItems",
                schema: "services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageItems_HealthcarePackages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "services",
                        principalTable: "HealthcarePackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServicePrices",
                schema: "services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicePrices_HealthcareServices_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "services",
                        principalTable: "HealthcareServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthcarePackages_OrganizationId_IsActive",
                schema: "services",
                table: "HealthcarePackages",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_HealthcarePackages_OrganizationId_PackageCode",
                schema: "services",
                table: "HealthcarePackages",
                columns: new[] { "OrganizationId", "PackageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthcareServices_OrganizationId_CategoryId",
                schema: "services",
                table: "HealthcareServices",
                columns: new[] { "OrganizationId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_HealthcareServices_OrganizationId_IsActive",
                schema: "services",
                table: "HealthcareServices",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_HealthcareServices_OrganizationId_ServiceCode",
                schema: "services",
                table: "HealthcareServices",
                columns: new[] { "OrganizationId", "ServiceCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackageItems_PackageId",
                schema: "services",
                table: "PackageItems",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageItems_PackageId_ServiceId",
                schema: "services",
                table: "PackageItems",
                columns: new[] { "PackageId", "ServiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCategories_OrganizationId_Code",
                schema: "services",
                table: "ServiceCategories",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCategories_OrganizationId_IsActive",
                schema: "services",
                table: "ServiceCategories",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrices_ServiceId",
                schema: "services",
                table: "ServicePrices",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrices_ServiceId_EffectiveToUtc",
                schema: "services",
                table: "ServicePrices",
                columns: new[] { "ServiceId", "EffectiveToUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageItems",
                schema: "services");

            migrationBuilder.DropTable(
                name: "ServiceCategories",
                schema: "services");

            migrationBuilder.DropTable(
                name: "ServicePrices",
                schema: "services");

            migrationBuilder.DropTable(
                name: "HealthcarePackages",
                schema: "services");

            migrationBuilder.DropTable(
                name: "HealthcareServices",
                schema: "services");
        }
    }
}
