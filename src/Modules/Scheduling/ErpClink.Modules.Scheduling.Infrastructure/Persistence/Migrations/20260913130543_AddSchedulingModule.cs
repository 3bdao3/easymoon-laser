using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.Scheduling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "scheduling");

            migrationBuilder.CreateTable(
                name: "ClinicHolidays",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicHolidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoctorScheduleExceptions",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    SlotDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorScheduleExceptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoctorWorkingSchedules",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    SlotDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorWorkingSchedules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicHolidays_OrganizationId_Date",
                schema: "scheduling",
                table: "ClinicHolidays",
                columns: new[] { "OrganizationId", "Date" },
                unique: true,
                filter: "[IsActive] = 1 AND [BranchId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicHolidays_OrganizationId_Date_IsActive",
                schema: "scheduling",
                table: "ClinicHolidays",
                columns: new[] { "OrganizationId", "Date", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorScheduleExceptions_BranchId",
                schema: "scheduling",
                table: "DoctorScheduleExceptions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorScheduleExceptions_OrganizationId_DoctorId_Date_IsActive",
                schema: "scheduling",
                table: "DoctorScheduleExceptions",
                columns: new[] { "OrganizationId", "DoctorId", "Date", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorWorkingSchedules_BranchId",
                schema: "scheduling",
                table: "DoctorWorkingSchedules",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorWorkingSchedules_ClinicId_IsActive",
                schema: "scheduling",
                table: "DoctorWorkingSchedules",
                columns: new[] { "ClinicId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorWorkingSchedules_OrganizationId_DoctorId_ClinicId_DayOfWeek_IsActive",
                schema: "scheduling",
                table: "DoctorWorkingSchedules",
                columns: new[] { "OrganizationId", "DoctorId", "ClinicId", "DayOfWeek", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorWorkingSchedules_OrganizationId_DoctorId_IsActive",
                schema: "scheduling",
                table: "DoctorWorkingSchedules",
                columns: new[] { "OrganizationId", "DoctorId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicHolidays",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "DoctorScheduleExceptions",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "DoctorWorkingSchedules",
                schema: "scheduling");
        }
    }
}
