using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.Queue.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQueueModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "queue");

            migrationBuilder.CreateTable(
                name: "QueueEntries",
                schema: "queue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueueNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CheckInTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CalledTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServiceStartTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServiceEndTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QueueNumberSequences",
                schema: "queue",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueNumberSequences", x => new { x.OrganizationId, x.BranchId, x.ClinicId, x.QueueDate });
                });

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_ActiveAppointment",
                schema: "queue",
                table: "QueueEntries",
                columns: new[] { "OrganizationId", "AppointmentId" },
                unique: true,
                filter: "[Status] IN ('Waiting', 'Called', 'InService')");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_BranchId",
                schema: "queue",
                table: "QueueEntries",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_OrganizationId_BranchId_QueueDate_ClinicId_QueueNumber",
                schema: "queue",
                table: "QueueEntries",
                columns: new[] { "OrganizationId", "BranchId", "QueueDate", "ClinicId", "QueueNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_OrganizationId_BranchId_QueueDate_Status",
                schema: "queue",
                table: "QueueEntries",
                columns: new[] { "OrganizationId", "BranchId", "QueueDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_OrganizationId_ClinicId_QueueDate",
                schema: "queue",
                table: "QueueEntries",
                columns: new[] { "OrganizationId", "ClinicId", "QueueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_OrganizationId_DoctorId_QueueDate",
                schema: "queue",
                table: "QueueEntries",
                columns: new[] { "OrganizationId", "DoctorId", "QueueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_OrganizationId_PatientId_QueueDate",
                schema: "queue",
                table: "QueueEntries",
                columns: new[] { "OrganizationId", "PatientId", "QueueDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueueEntries",
                schema: "queue");

            migrationBuilder.DropTable(
                name: "QueueNumberSequences",
                schema: "queue");
        }
    }
}
