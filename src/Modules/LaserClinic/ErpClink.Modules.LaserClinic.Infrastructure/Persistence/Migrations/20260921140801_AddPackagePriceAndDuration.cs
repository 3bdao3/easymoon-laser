using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.LaserClinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPackagePriceAndDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PackageDurationTotalMinutes",
                schema: "laser",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PackagePriceTotal",
                schema: "laser",
                table: "Customers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackageDurationTotalMinutes",
                schema: "laser",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PackagePriceTotal",
                schema: "laser",
                table: "Customers");
        }
    }
}
