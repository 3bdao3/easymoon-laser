using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingAndPaymentsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.CreateTable(
                name: "InvoiceNumberSequences",
                schema: "billing",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceNumberSequences", x => new { x.OrganizationId, x.Year });
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalVisitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IssuedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    VoidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentNumberSequences",
                schema: "billing",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentNumberSequences", x => new { x.OrganizationId, x.Year });
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReversedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReversedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ReversalReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServicePriceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DescriptionSnapshot = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ServiceCodeSnapshot = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineSubtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "billing",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId",
                schema: "billing",
                table: "InvoiceLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_PackageId",
                schema: "billing",
                table: "InvoiceLines",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_ServiceId",
                schema: "billing",
                table: "InvoiceLines",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BranchId",
                schema: "billing",
                table: "Invoices",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_MedicalVisitId",
                schema: "billing",
                table: "Invoices",
                column: "MedicalVisitId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrganizationId_InvoiceNumber",
                schema: "billing",
                table: "Invoices",
                columns: new[] { "OrganizationId", "InvoiceNumber" },
                unique: true,
                filter: "[InvoiceNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrganizationId_PatientId_InvoiceDate",
                schema: "billing",
                table: "Invoices",
                columns: new[] { "OrganizationId", "PatientId", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrganizationId_Status_InvoiceDate",
                schema: "billing",
                table: "Invoices",
                columns: new[] { "OrganizationId", "Status", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BranchId",
                schema: "billing",
                table: "Payments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrganizationId_InvoiceId_PaymentDate",
                schema: "billing",
                table: "Payments",
                columns: new[] { "OrganizationId", "InvoiceId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrganizationId_PaymentNumber",
                schema: "billing",
                table: "Payments",
                columns: new[] { "OrganizationId", "PaymentNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceLines",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "InvoiceNumberSequences",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "PaymentNumberSequences",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "Invoices",
                schema: "billing");
        }
    }
}
