using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.Prescriptions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "prescriptions");

            migrationBuilder.CreateTable(
                name: "Medications",
                schema: "prescriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GenericName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Strength = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DosageForm = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Route = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionNumberSequences",
                schema: "prescriptions",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionNumberSequences", x => new { x.OrganizationId, x.Year });
                });

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                schema: "prescriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MedicalVisitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IssuedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionItems",
                schema: "prescriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicationNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Dosage = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Route = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalSchema: "prescriptions",
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Medications_OrganizationId_Code",
                schema: "prescriptions",
                table: "Medications",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Medications_OrganizationId_GenericName",
                schema: "prescriptions",
                table: "Medications",
                columns: new[] { "OrganizationId", "GenericName" });

            migrationBuilder.CreateIndex(
                name: "IX_Medications_OrganizationId_IsActive",
                schema: "prescriptions",
                table: "Medications",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Medications_OrganizationId_Name",
                schema: "prescriptions",
                table: "Medications",
                columns: new[] { "OrganizationId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_MedicationId",
                schema: "prescriptions",
                table: "PrescriptionItems",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionId",
                schema: "prescriptions",
                table: "PrescriptionItems",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_ActiveMedicalVisit",
                schema: "prescriptions",
                table: "Prescriptions",
                columns: new[] { "OrganizationId", "MedicalVisitId" },
                unique: true,
                filter: "[Status] IN ('Draft', 'Issued')");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_BranchId",
                schema: "prescriptions",
                table: "Prescriptions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_OrganizationId_DoctorId_PrescriptionDate",
                schema: "prescriptions",
                table: "Prescriptions",
                columns: new[] { "OrganizationId", "DoctorId", "PrescriptionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_OrganizationId_PatientId_PrescriptionDate",
                schema: "prescriptions",
                table: "Prescriptions",
                columns: new[] { "OrganizationId", "PatientId", "PrescriptionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_OrganizationId_PrescriptionNumber",
                schema: "prescriptions",
                table: "Prescriptions",
                columns: new[] { "OrganizationId", "PrescriptionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_OrganizationId_Status_PrescriptionDate",
                schema: "prescriptions",
                table: "Prescriptions",
                columns: new[] { "OrganizationId", "Status", "PrescriptionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Medications",
                schema: "prescriptions");

            migrationBuilder.DropTable(
                name: "PrescriptionItems",
                schema: "prescriptions");

            migrationBuilder.DropTable(
                name: "PrescriptionNumberSequences",
                schema: "prescriptions");

            migrationBuilder.DropTable(
                name: "Prescriptions",
                schema: "prescriptions");
        }
    }
}
