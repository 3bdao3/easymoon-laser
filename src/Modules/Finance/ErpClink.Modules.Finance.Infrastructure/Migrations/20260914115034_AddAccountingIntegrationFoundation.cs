using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.Finance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingIntegrationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountingIntegrationRequests",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceModule = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingIntegrationRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingIntegrationRequests_CorrelationId",
                schema: "finance",
                table: "AccountingIntegrationRequests",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingIntegrationRequests_Org_SourceLookup",
                schema: "finance",
                table: "AccountingIntegrationRequests",
                columns: new[] { "OrganizationId", "SourceModule", "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingIntegrationRequests_OrganizationId",
                schema: "finance",
                table: "AccountingIntegrationRequests",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "UX_AccountingIntegrationRequests_Org_IdempotencyKey",
                schema: "finance",
                table: "AccountingIntegrationRequests",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AccountingIntegrationRequests_Org_Source_Event_Key",
                schema: "finance",
                table: "AccountingIntegrationRequests",
                columns: new[] { "OrganizationId", "SourceModule", "SourceType", "SourceId", "EventType", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingIntegrationRequests",
                schema: "finance");
        }
    }
}
