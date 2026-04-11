using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSessionService.Infrastructure.Migrations;

public partial class AddPodcastRequest : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "podcast_requests",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                live_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                script_text = table.Column<string>(type: "text", nullable: false),
                audio_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                duration_seconds = table.Column<int>(type: "integer", nullable: false),
                voice_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                voice_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                azuracast_media_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                reject_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_podcast_requests", x => x.id);
                table.ForeignKey(
                    name: "FK_podcast_requests_live_sessions_live_session_id",
                    column: x => x.live_session_id,
                    principalTable: "live_sessions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_podcast_requests_live_session_id",
            table: "podcast_requests",
            column: "live_session_id");

        migrationBuilder.CreateIndex(
            name: "ix_podcast_requests_requested_at",
            table: "podcast_requests",
            column: "requested_at");

        migrationBuilder.CreateIndex(
            name: "ix_podcast_requests_requested_by_user_id",
            table: "podcast_requests",
            column: "requested_by_user_id");

        migrationBuilder.CreateIndex(
            name: "ix_podcast_requests_status",
            table: "podcast_requests",
            column: "status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "podcast_requests");
    }
}