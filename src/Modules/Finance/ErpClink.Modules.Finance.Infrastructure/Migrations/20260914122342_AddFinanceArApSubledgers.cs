using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.Finance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceArApSubledgers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubledgerPartyBalances",
                schema: "finance",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubledgerType = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    PartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubledgerPartyBalances", x => new { x.OrganizationId, x.SubledgerType, x.PartyId });
                });

            migrationBuilder.CreateTable(
                name: "SubledgerTransactions",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubledgerType = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    PartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceModule = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PartyDisplayNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ReversalOfTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReversedByTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubledgerTransactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubledgerPartyBalances_Org_Type",
                schema: "finance",
                table: "SubledgerPartyBalances",
                columns: new[] { "OrganizationId", "SubledgerType" });

            migrationBuilder.CreateIndex(
                name: "IX_SubledgerTransactions_CorrelationId",
                schema: "finance",
                table: "SubledgerTransactions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubledgerTransactions_Org_Type_Date",
                schema: "finance",
                table: "SubledgerTransactions",
                columns: new[] { "OrganizationId", "SubledgerType", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SubledgerTransactions_Org_Type_Party_Date",
                schema: "finance",
                table: "SubledgerTransactions",
                columns: new[] { "OrganizationId", "SubledgerType", "PartyId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "UX_SubledgerTransactions_Org_IdempotencyKey",
                schema: "finance",
                table: "SubledgerTransactions",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SubledgerTransactions_Org_Source_Event_Key",
                schema: "finance",
                table: "SubledgerTransactions",
                columns: new[] { "OrganizationId", "SourceModule", "SourceType", "SourceId", "EventType", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubledgerPartyBalances",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "SubledgerTransactions",
                schema: "finance");
        }
    }
}
