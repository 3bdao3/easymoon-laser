using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.Appointments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IncludeCheckedInInActiveSlotIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_ActiveSlot",
                schema: "appointments",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ActiveSlot",
                schema: "appointments",
                table: "Appointments",
                columns: new[] { "OrganizationId", "DoctorId", "ClinicId", "AppointmentDate", "StartTime" },
                unique: true,
                filter: "[Status] IN ('Scheduled', 'Confirmed', 'CheckedIn')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_ActiveSlot",
                schema: "appointments",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ActiveSlot",
                schema: "appointments",
                table: "Appointments",
                columns: new[] { "OrganizationId", "DoctorId", "ClinicId", "AppointmentDate", "StartTime" },
                unique: true,
                filter: "[Status] IN ('Scheduled', 'Confirmed')");
        }
    }
}
