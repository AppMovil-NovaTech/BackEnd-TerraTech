using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaTech.TerraTech.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AgriculturalAnalyticsAndProjections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_reports__device_device_id",
                table: "reports");

            migrationBuilder.DropIndex(
                name: "i_x_reports_device_id",
                table: "reports");

            migrationBuilder.AddColumn<double>(
                name: "confidence_score",
                table: "reports",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "crop_name",
                table: "reports",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "environmental_factor",
                table: "reports",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "factor_reason",
                table: "reports",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "field_id",
                table: "reports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "historical_kg",
                table: "reports",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "projected_yield_kg",
                table: "reports",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "i_x_reports_field_id",
                table: "reports",
                column: "field_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_reports__field_field_id",
                table: "reports",
                column: "field_id",
                principalTable: "fields",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_reports__field_field_id",
                table: "reports");

            migrationBuilder.DropIndex(
                name: "i_x_reports_field_id",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "confidence_score",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "crop_name",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "environmental_factor",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "factor_reason",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "field_id",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "historical_kg",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "projected_yield_kg",
                table: "reports");

            migrationBuilder.CreateIndex(
                name: "i_x_reports_device_id",
                table: "reports",
                column: "device_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_reports__device_device_id",
                table: "reports",
                column: "device_id",
                principalTable: "devices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
