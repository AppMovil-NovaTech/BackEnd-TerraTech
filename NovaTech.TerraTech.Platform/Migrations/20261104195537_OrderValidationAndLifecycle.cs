using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaTech.TerraTech.Platform.Migrations
{
    /// <inheritdoc />
    public partial class OrderValidationAndLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_orders__profile_profile_id",
                table: "orders");

            migrationBuilder.RenameColumn(
                name: "profile_id",
                table: "orders",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "i_x_orders_profile_id",
                table: "orders",
                newName: "i_x_orders_user_id");

            migrationBuilder.Sql("UPDATE orders o INNER JOIN profiles p ON o.user_id = p.id SET o.user_id = p.user_id;");

            migrationBuilder.AddForeignKey(
                name: "f_k_orders_users_user_id",
                table: "orders",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_orders_users_user_id",
                table: "orders");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "orders",
                newName: "profile_id");

            migrationBuilder.RenameIndex(
                name: "i_x_orders_user_id",
                table: "orders",
                newName: "i_x_orders_profile_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_orders__profile_profile_id",
                table: "orders",
                column: "profile_id",
                principalTable: "profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
