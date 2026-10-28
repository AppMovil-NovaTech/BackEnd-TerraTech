using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaTech.TerraTech.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddFertilityZoneThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "moisture_max",
                table: "fertility_zones",
                type: "double",
                nullable: false,
                defaultValue: 75.0);

            migrationBuilder.AddColumn<double>(
                name: "moisture_min",
                table: "fertility_zones",
                type: "double",
                nullable: false,
                defaultValue: 25.0);

            migrationBuilder.AddColumn<double>(
                name: "nitrogen_max",
                table: "fertility_zones",
                type: "double",
                nullable: false,
                defaultValue: 40.0);

            migrationBuilder.AddColumn<double>(
                name: "nitrogen_min",
                table: "fertility_zones",
                type: "double",
                nullable: false,
                defaultValue: 20.0);

            migrationBuilder.AddColumn<double>(
                name: "phosphorus_max",
                table: "fertility_zones",
                type: "double",
                nullable: false,
                defaultValue: 30.0);

            migrationBuilder.AddColumn<double>(
                name: "phosphorus_min",
                table: "fertility_zones",
                type: "double",
                nullable: false,
                defaultValue: 15.0);

            migrationBuilder.AddColumn<double>(
                name: "potassium_max",
                table: "fertility_zones",
                type: "double",
                nullable: false,
                defaultValue: 50.0);

            migrationBuilder.AddColumn<double>(
                name: "potassium_min",
                table: "fertility_zones",
                type: "double",
                nullable: false,
                defaultValue: 30.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "moisture_max",
                table: "fertility_zones");

            migrationBuilder.DropColumn(
                name: "moisture_min",
                table: "fertility_zones");

            migrationBuilder.DropColumn(
                name: "nitrogen_max",
                table: "fertility_zones");

            migrationBuilder.DropColumn(
                name: "nitrogen_min",
                table: "fertility_zones");

            migrationBuilder.DropColumn(
                name: "phosphorus_max",
                table: "fertility_zones");

            migrationBuilder.DropColumn(
                name: "phosphorus_min",
                table: "fertility_zones");

            migrationBuilder.DropColumn(
                name: "potassium_max",
                table: "fertility_zones");

            migrationBuilder.DropColumn(
                name: "potassium_min",
                table: "fertility_zones");
        }
    }
}
