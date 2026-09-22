using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.MedicalVisits.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalVisitsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "medical_visits");

            migrationBuilder.CreateTable(
                name: "MedicalVisits",
                schema: "medical_visits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisitNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueueEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VisitDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ChiefComplaint = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ClinicalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExaminationFindings = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DiagnosisNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FollowUpNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalVisits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitNumberSequences",
                schema: "medical_visits",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitNumberSequences", x => new { x.OrganizationId, x.Year });
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalVisits_ActiveAppointment",
                schema: "medical_visits",
                table: "MedicalVisits",
                columns: new[] { "OrganizationId", "AppointmentId" },
                unique: true,
                filter: "[Status] IN ('Open', 'InProgress')");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalVisits_BranchId",
                schema: "medical_visits",
                table: "MedicalVisits",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalVisits_OrganizationId_ClinicId_VisitDate",
                schema: "medical_visits",
                table: "MedicalVisits",
                columns: new[] { "OrganizationId", "ClinicId", "VisitDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalVisits_OrganizationId_DoctorId_VisitDate",
                schema: "medical_visits",
                table: "MedicalVisits",
                columns: new[] { "OrganizationId", "DoctorId", "VisitDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalVisits_OrganizationId_PatientId_VisitDate",
                schema: "medical_visits",
                table: "MedicalVisits",
                columns: new[] { "OrganizationId", "PatientId", "VisitDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalVisits_OrganizationId_Status_VisitDate",
                schema: "medical_visits",
                table: "MedicalVisits",
                columns: new[] { "OrganizationId", "Status", "VisitDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalVisits_OrganizationId_VisitNumber",
                schema: "medical_visits",
                table: "MedicalVisits",
                columns: new[] { "OrganizationId", "VisitNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalVisits_QueueEntryId",
                schema: "medical_visits",
                table: "MedicalVisits",
                column: "QueueEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicalVisits",
                schema: "medical_visits");

            migrationBuilder.DropTable(
                name: "VisitNumberSequences",
                schema: "medical_visits");
        }
    }
}
