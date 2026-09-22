using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.Procurement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderReceivingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                schema: "procurement",
                table: "PurchaseOrderLines",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityReceived",
                schema: "procurement",
                table: "PurchaseOrderLines",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuantityReceived",
                schema: "procurement",
                table: "PurchaseOrderLines");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                schema: "procurement",
                table: "PurchaseOrderLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldPrecision: 18,
                oldScale: 3);
        }
    }
}
