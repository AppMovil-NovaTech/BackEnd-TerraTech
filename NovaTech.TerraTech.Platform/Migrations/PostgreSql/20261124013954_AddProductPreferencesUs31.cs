using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NovaTech.TerraTech.Platform.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddProductPreferencesUs31 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_preferences",
                schema: "terratech",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_product_preferences", x => x.id);
                    table.ForeignKey(
                        name: "f_k_product_preferences_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "terratech",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_product_preferences_category",
                schema: "terratech",
                table: "product_preferences",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "i_x_product_preferences_user_id_category",
                schema: "terratech",
                table: "product_preferences",
                columns: new[] { "user_id", "category" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_preferences",
                schema: "terratech");
        }
    }
}
