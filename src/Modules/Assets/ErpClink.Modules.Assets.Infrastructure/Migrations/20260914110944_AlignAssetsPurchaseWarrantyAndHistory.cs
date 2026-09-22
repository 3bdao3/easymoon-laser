using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpClink.Modules.Assets.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignAssetsPurchaseWarrantyAndHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AcquisitionDate",
                schema: "assets",
                table: "Assets",
                newName: "PurchaseDate");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "PurchaseDate",
                schema: "assets",
                table: "Assets",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.RenameColumn(
                name: "WarrantyExpiryDate",
                schema: "assets",
                table: "Assets",
                newName: "WarrantyEndDate");

            migrationBuilder.AddColumn<DateOnly>(
                name: "WarrantyStartDate",
                schema: "assets",
                table: "Assets",
                type: "date",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "Metadata",
                schema: "assets",
                table: "AssetHistory");

            migrationBuilder.AddColumn<string>(
                name: "NewValue",
                schema: "assets",
                table: "AssetHistory",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                schema: "assets",
                table: "AssetHistory",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OldValue",
                schema: "assets",
                table: "AssetHistory",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WarrantyStartDate",
                schema: "assets",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "NewValue",
                schema: "assets",
                table: "AssetHistory");

            migrationBuilder.DropColumn(
                name: "Notes",
                schema: "assets",
                table: "AssetHistory");

            migrationBuilder.DropColumn(
                name: "OldValue",
                schema: "assets",
                table: "AssetHistory");

            migrationBuilder.RenameColumn(
                name: "WarrantyEndDate",
                schema: "assets",
                table: "Assets",
                newName: "WarrantyExpiryDate");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "PurchaseDate",
                schema: "assets",
                table: "Assets",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "PurchaseDate",
                schema: "assets",
                table: "Assets",
                newName: "AcquisitionDate");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                schema: "assets",
                table: "AssetHistory",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }
    }
}
