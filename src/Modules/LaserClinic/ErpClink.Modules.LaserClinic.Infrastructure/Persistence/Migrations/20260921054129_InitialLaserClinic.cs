using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.LaserClinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialLaserClinic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "laser");

            migrationBuilder.CreateTable(
                name: "Appointments",
                schema: "laser",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicSettings",
                schema: "laser",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpeningTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    ClosingTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    AppointmentSlotIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    DefaultBufferMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "laser",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Age = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LaserServices",
                schema: "laser",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MinDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaserServices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentServices",
                schema: "laser",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LaserServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentServices_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalSchema: "laser",
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentServices_LaserServices_LaserServiceId",
                        column: x => x.LaserServiceId,
                        principalSchema: "laser",
                        principalTable: "LaserServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentDate",
                schema: "laser",
                table: "Appointments",
                column: "AppointmentDate");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentDate_StartTime",
                schema: "laser",
                table: "Appointments",
                columns: new[] { "AppointmentDate", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CustomerId",
                schema: "laser",
                table: "Appointments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentServices_AppointmentId",
                schema: "laser",
                table: "AppointmentServices",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentServices_LaserServiceId",
                schema: "laser",
                table: "AppointmentServices",
                column: "LaserServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_FullName",
                schema: "laser",
                table: "Customers",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PhoneNumber",
                schema: "laser",
                table: "Customers",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_LaserServices_DisplayOrder",
                schema: "laser",
                table: "LaserServices",
                column: "DisplayOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentServices",
                schema: "laser");

            migrationBuilder.DropTable(
                name: "ClinicSettings",
                schema: "laser");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "laser");

            migrationBuilder.DropTable(
                name: "Appointments",
                schema: "laser");

            migrationBuilder.DropTable(
                name: "LaserServices",
                schema: "laser");
        }
    }
}
