using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.Doctors.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorsAndClinicsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "doctors");

            migrationBuilder.CreateTable(
                name: "Clinics",
                schema: "doctors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clinics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoctorNumberSequences",
                schema: "doctors",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorNumberSequences", x => new { x.OrganizationId, x.Year });
                });

            migrationBuilder.CreateTable(
                name: "Doctors",
                schema: "doctors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SpecialtyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LicenseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Nurses",
                schema: "doctors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NurseNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nurses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Specialties",
                schema: "doctors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoctorClinicAssignments",
                schema: "doctors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorClinicAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorClinicAssignments_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalSchema: "doctors",
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoctorClinicAssignments_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalSchema: "doctors",
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_BranchId",
                schema: "doctors",
                table: "Clinics",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_OrganizationId_Code",
                schema: "doctors",
                table: "Clinics",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_OrganizationId_IsActive",
                schema: "doctors",
                table: "Clinics",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorClinicAssignments_ClinicId",
                schema: "doctors",
                table: "DoctorClinicAssignments",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorClinicAssignments_DoctorId_ClinicId",
                schema: "doctors",
                table: "DoctorClinicAssignments",
                columns: new[] { "DoctorId", "ClinicId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorClinicAssignments_OrganizationId_DoctorId_ClinicId_IsActive",
                schema: "doctors",
                table: "DoctorClinicAssignments",
                columns: new[] { "OrganizationId", "DoctorId", "ClinicId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_BranchId",
                schema: "doctors",
                table: "Doctors",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_OrganizationId_DoctorNumber",
                schema: "doctors",
                table: "Doctors",
                columns: new[] { "OrganizationId", "DoctorNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_OrganizationId_IsActive",
                schema: "doctors",
                table: "Doctors",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_OrganizationId_LastName_FirstName",
                schema: "doctors",
                table: "Doctors",
                columns: new[] { "OrganizationId", "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_OrganizationId_SpecialtyId",
                schema: "doctors",
                table: "Doctors",
                columns: new[] { "OrganizationId", "SpecialtyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_OrganizationId_UserId",
                schema: "doctors",
                table: "Doctors",
                columns: new[] { "OrganizationId", "UserId" },
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Nurses_OrganizationId_IsActive",
                schema: "doctors",
                table: "Nurses",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Nurses_OrganizationId_NurseNumber",
                schema: "doctors",
                table: "Nurses",
                columns: new[] { "OrganizationId", "NurseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_OrganizationId_Code",
                schema: "doctors",
                table: "Specialties",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_OrganizationId_IsActive",
                schema: "doctors",
                table: "Specialties",
                columns: new[] { "OrganizationId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorClinicAssignments",
                schema: "doctors");

            migrationBuilder.DropTable(
                name: "DoctorNumberSequences",
                schema: "doctors");

            migrationBuilder.DropTable(
                name: "Nurses",
                schema: "doctors");

            migrationBuilder.DropTable(
                name: "Specialties",
                schema: "doctors");

            migrationBuilder.DropTable(
                name: "Clinics",
                schema: "doctors");

            migrationBuilder.DropTable(
                name: "Doctors",
                schema: "doctors");
        }
    }
}
