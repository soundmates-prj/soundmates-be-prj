using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSessionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMediaFileLocalFilePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AzuraCastMediaId",
                table: "media_files",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_files_AzuraCastMediaId",
                table: "media_files",
                column: "AzuraCastMediaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_media_files_AzuraCastMediaId",
                table: "media_files");

            migrationBuilder.DropColumn(
                name: "AzuraCastMediaId",
                table: "media_files");
        }
    }
}
