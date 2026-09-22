using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.LaserClinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLaserOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LaserOffers",
                schema: "laser",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaserOffers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LaserOffers_DisplayOrder",
                schema: "laser",
                table: "LaserOffers",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_LaserOffers_IsActive",
                schema: "laser",
                table: "LaserOffers",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LaserOffers",
                schema: "laser");
        }
    }
}
