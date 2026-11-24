using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaTech.TerraTech.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddProductInterestUs29 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "consultation_month",
                table: "products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "consultation_year",
                table: "products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "high_interest_alert_sent",
                table: "products",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "inactivity_alert_sent",
                table: "products",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "interest_threshold",
                table: "products",
                type: "int",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_consultation_at",
                table: "products",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "monthly_consultations",
                table: "products",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "consultation_month",
                table: "products");

            migrationBuilder.DropColumn(
                name: "consultation_year",
                table: "products");

            migrationBuilder.DropColumn(
                name: "high_interest_alert_sent",
                table: "products");

            migrationBuilder.DropColumn(
                name: "inactivity_alert_sent",
                table: "products");

            migrationBuilder.DropColumn(
                name: "interest_threshold",
                table: "products");

            migrationBuilder.DropColumn(
                name: "last_consultation_at",
                table: "products");

            migrationBuilder.DropColumn(
                name: "monthly_consultations",
                table: "products");
        }
    }
}
