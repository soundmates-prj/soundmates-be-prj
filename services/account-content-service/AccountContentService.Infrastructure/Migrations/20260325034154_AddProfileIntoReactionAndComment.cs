using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountContentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileIntoReactionAndComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserAvatarUrl",
                table: "PostReactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserFullName",
                table: "PostReactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserAvatarUrl",
                table: "BlogComments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserFullName",
                table: "BlogComments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserAvatarUrl",
                table: "PostReactions");

            migrationBuilder.DropColumn(
                name: "UserFullName",
                table: "PostReactions");

            migrationBuilder.DropColumn(
                name: "UserAvatarUrl",
                table: "BlogComments");

            migrationBuilder.DropColumn(
                name: "UserFullName",
                table: "BlogComments");
        }
    }
}
