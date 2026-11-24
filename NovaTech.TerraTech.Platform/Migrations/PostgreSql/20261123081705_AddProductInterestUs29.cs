using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaTech.TerraTech.Platform.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddProductInterestUs29 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "consultation_month",
                schema: "terratech",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "consultation_year",
                schema: "terratech",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "high_interest_alert_sent",
                schema: "terratech",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "inactivity_alert_sent",
                schema: "terratech",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "interest_threshold",
                schema: "terratech",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_consultation_at",
                schema: "terratech",
                table: "products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "monthly_consultations",
                schema: "terratech",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "consultation_month",
                schema: "terratech",
                table: "products");

            migrationBuilder.DropColumn(
                name: "consultation_year",
                schema: "terratech",
                table: "products");

            migrationBuilder.DropColumn(
                name: "high_interest_alert_sent",
                schema: "terratech",
                table: "products");

            migrationBuilder.DropColumn(
                name: "inactivity_alert_sent",
                schema: "terratech",
                table: "products");

            migrationBuilder.DropColumn(
                name: "interest_threshold",
                schema: "terratech",
                table: "products");

            migrationBuilder.DropColumn(
                name: "last_consultation_at",
                schema: "terratech",
                table: "products");

            migrationBuilder.DropColumn(
                name: "monthly_consultations",
                schema: "terratech",
                table: "products");
        }
    }
}
