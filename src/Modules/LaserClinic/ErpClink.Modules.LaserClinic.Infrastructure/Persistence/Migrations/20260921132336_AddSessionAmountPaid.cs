using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.LaserClinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionAmountPaid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                schema: "laser",
                table: "Appointments",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountPaid",
                schema: "laser",
                table: "Appointments");
        }
    }
}
