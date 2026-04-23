using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSessionService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPodcastRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "audio_url",
                table: "podcast_requests");

            migrationBuilder.DropColumn(
                name: "episode_title",
                table: "podcast_requests");

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "podcast_requests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "type",
                table: "podcast_requests");

            migrationBuilder.AddColumn<string>(
                name: "audio_url",
                table: "podcast_requests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "episode_title",
                table: "podcast_requests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
