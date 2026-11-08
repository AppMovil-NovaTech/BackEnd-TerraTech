using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaTech.TerraTech.Platform.Migrations
{
    /// <inheritdoc />
    public partial class StockCatalogSynchronization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "warehouse_location",
                table: "inventories",
                newName: "name");

            migrationBuilder.AddColumn<decimal>(
                name: "average_weight_per_unit_kg",
                table: "inventories",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "field_id",
                table: "inventories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "price",
                table: "inventories",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "profile_id",
                table: "inventories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "total_weight_kg",
                table: "inventories",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "unit_type",
                table: "inventories",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "average_weight_per_unit_kg",
                table: "inventories");

            migrationBuilder.DropColumn(
                name: "field_id",
                table: "inventories");

            migrationBuilder.DropColumn(
                name: "price",
                table: "inventories");

            migrationBuilder.DropColumn(
                name: "profile_id",
                table: "inventories");

            migrationBuilder.DropColumn(
                name: "total_weight_kg",
                table: "inventories");

            migrationBuilder.DropColumn(
                name: "unit_type",
                table: "inventories");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "inventories",
                newName: "warehouse_location");
        }
    }
}
