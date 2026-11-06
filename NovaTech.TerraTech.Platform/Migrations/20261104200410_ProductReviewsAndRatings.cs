using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaTech.TerraTech.Platform.Migrations
{
    /// <inheritdoc />
    public partial class ProductReviewsAndRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_comments__profile_author_profile_id",
                table: "comments");

            migrationBuilder.RenameColumn(
                name: "author_profile_id",
                table: "comments",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "i_x_comments_author_profile_id",
                table: "comments",
                newName: "i_x_comments_user_id");

            migrationBuilder.AddColumn<int>(
                name: "product_id",
                table: "comments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "i_x_comments_product_id",
                table: "comments",
                column: "product_id");

            migrationBuilder.Sql("UPDATE comments c INNER JOIN profiles p ON c.user_id = p.id SET c.user_id = p.user_id;");
            migrationBuilder.Sql("DELETE FROM comments WHERE product_id = 0;");

            migrationBuilder.AddForeignKey(
                name: "f_k_comments_products_product_id",
                table: "comments",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_comments_users_user_id",
                table: "comments",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_comments_products_product_id",
                table: "comments");

            migrationBuilder.DropForeignKey(
                name: "f_k_comments_users_user_id",
                table: "comments");

            migrationBuilder.DropIndex(
                name: "i_x_comments_product_id",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "product_id",
                table: "comments");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "comments",
                newName: "author_profile_id");

            migrationBuilder.RenameIndex(
                name: "i_x_comments_user_id",
                table: "comments",
                newName: "i_x_comments_author_profile_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_comments__profile_author_profile_id",
                table: "comments",
                column: "author_profile_id",
                principalTable: "profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
