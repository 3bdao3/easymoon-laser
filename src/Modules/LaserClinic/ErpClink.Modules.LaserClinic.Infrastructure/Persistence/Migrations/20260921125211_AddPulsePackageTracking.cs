using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.LaserClinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPulsePackageTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PulsePackageRemaining",
                schema: "laser",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PulsePackageTotal",
                schema: "laser",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PulsesConsumed",
                schema: "laser",
                table: "AppointmentServices",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PulsePackageRemaining",
                schema: "laser",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PulsePackageTotal",
                schema: "laser",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PulsesConsumed",
                schema: "laser",
                table: "AppointmentServices");
        }
    }
}
