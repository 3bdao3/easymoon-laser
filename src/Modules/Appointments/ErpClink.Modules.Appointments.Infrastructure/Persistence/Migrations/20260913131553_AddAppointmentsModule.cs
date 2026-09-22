using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.Appointments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "appointments");

            migrationBuilder.CreateTable(
                name: "AppointmentNumberSequences",
                schema: "appointments",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentNumberSequences", x => new { x.OrganizationId, x.Year });
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                schema: "appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentRescheduleHistory",
                schema: "appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PreviousStart = table.Column<TimeOnly>(type: "time", nullable: false),
                    PreviousEnd = table.Column<TimeOnly>(type: "time", nullable: false),
                    NewDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NewStart = table.Column<TimeOnly>(type: "time", nullable: false),
                    NewEnd = table.Column<TimeOnly>(type: "time", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentRescheduleHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentRescheduleHistory_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalSchema: "appointments",
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentStatusHistory",
                schema: "appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentStatusHistory_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalSchema: "appointments",
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRescheduleHistory_AppointmentId",
                schema: "appointments",
                table: "AppointmentRescheduleHistory",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ActiveSlot",
                schema: "appointments",
                table: "Appointments",
                columns: new[] { "OrganizationId", "DoctorId", "ClinicId", "AppointmentDate", "StartTime" },
                unique: true,
                filter: "[Status] IN ('Scheduled', 'Confirmed')");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_BranchId",
                schema: "appointments",
                table: "Appointments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_OrganizationId_AppointmentNumber",
                schema: "appointments",
                table: "Appointments",
                columns: new[] { "OrganizationId", "AppointmentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_OrganizationId_ClinicId_AppointmentDate",
                schema: "appointments",
                table: "Appointments",
                columns: new[] { "OrganizationId", "ClinicId", "AppointmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_OrganizationId_DoctorId_AppointmentDate",
                schema: "appointments",
                table: "Appointments",
                columns: new[] { "OrganizationId", "DoctorId", "AppointmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_OrganizationId_PatientId_AppointmentDate",
                schema: "appointments",
                table: "Appointments",
                columns: new[] { "OrganizationId", "PatientId", "AppointmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_OrganizationId_Status_AppointmentDate",
                schema: "appointments",
                table: "Appointments",
                columns: new[] { "OrganizationId", "Status", "AppointmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentStatusHistory_AppointmentId",
                schema: "appointments",
                table: "AppointmentStatusHistory",
                column: "AppointmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentNumberSequences",
                schema: "appointments");

            migrationBuilder.DropTable(
                name: "AppointmentRescheduleHistory",
                schema: "appointments");

            migrationBuilder.DropTable(
                name: "AppointmentStatusHistory",
                schema: "appointments");

            migrationBuilder.DropTable(
                name: "Appointments",
                schema: "appointments");
        }
    }
}
