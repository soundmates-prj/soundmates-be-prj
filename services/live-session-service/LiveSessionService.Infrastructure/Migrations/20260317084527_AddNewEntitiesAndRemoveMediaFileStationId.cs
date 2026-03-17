using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSessionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewEntitiesAndRemoveMediaFileStationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StationId",
                table: "media_files");

            migrationBuilder.CreateTable(
                name: "live_session_chats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    live_session_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_live_session_chats", x => x.id);
                    table.ForeignKey(
                        name: "FK_live_session_chats_live_sessions_live_session_id",
                        column: x => x.live_session_id,
                        principalTable: "live_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "podcasts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    author = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    banner = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_podcasts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "session_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    live_session_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_session_schedules_live_sessions_live_session_id",
                        column: x => x.live_session_id,
                        principalTable: "live_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "song_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    live_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reject_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_song_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_song_requests_live_sessions_live_session_id",
                        column: x => x.live_session_id,
                        principalTable: "live_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_song_requests_media_files_media_file_id",
                        column: x => x.media_file_id,
                        principalTable: "media_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_playlists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_playlist_id = table.Column<int>(type: "integer", nullable: false),
                    playlist_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    playlist_order = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    include_in_requests = table.Column<bool>(type: "boolean", nullable: false),
                    include_in_on_demand = table.Column<bool>(type: "boolean", nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_playlists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "podcast_episodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    audio_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    episode_number = table.Column<int>(type: "integer", nullable: false),
                    publish_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration = table.Column<int>(type: "integer", nullable: false),
                    podcast_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_podcast_episodes", x => x.id);
                    table.ForeignKey(
                        name: "FK_podcast_episodes_podcasts_podcast_id",
                        column: x => x.podcast_id,
                        principalTable: "podcasts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_playlist_medias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_playlist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_playlist_medias", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_playlist_medias_media_files_media_file_id",
                        column: x => x.media_file_id,
                        principalTable: "media_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_playlist_medias_user_playlists_user_playlist_id",
                        column: x => x.user_playlist_id,
                        principalTable: "user_playlists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_live_session_chats_created_at",
                table: "live_session_chats",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_live_session_chats_live_session_id",
                table: "live_session_chats",
                column: "live_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_podcast_episodes_podcast_id",
                table: "podcast_episodes",
                column: "podcast_id");

            migrationBuilder.CreateIndex(
                name: "IX_podcast_episodes_podcast_id_episode_number",
                table: "podcast_episodes",
                columns: new[] { "podcast_id", "episode_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_podcasts_created_by",
                table: "podcasts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_podcasts_status",
                table: "podcasts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_session_schedules_live_session_id",
                table: "session_schedules",
                column: "live_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_session_schedules_start_time",
                table: "session_schedules",
                column: "start_time");

            migrationBuilder.CreateIndex(
                name: "IX_song_requests_live_session_id",
                table: "song_requests",
                column: "live_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_song_requests_media_file_id",
                table: "song_requests",
                column: "media_file_id");

            migrationBuilder.CreateIndex(
                name: "IX_song_requests_requested_at",
                table: "song_requests",
                column: "requested_at");

            migrationBuilder.CreateIndex(
                name: "IX_song_requests_status",
                table: "song_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_user_playlist_medias_media_file_id",
                table: "user_playlist_medias",
                column: "media_file_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_playlist_medias_user_playlist_id",
                table: "user_playlist_medias",
                column: "user_playlist_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_playlists_external_playlist_id",
                table: "user_playlists",
                column: "external_playlist_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_playlists_user_id",
                table: "user_playlists",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "live_session_chats");

            migrationBuilder.DropTable(
                name: "podcast_episodes");

            migrationBuilder.DropTable(
                name: "session_schedules");

            migrationBuilder.DropTable(
                name: "song_requests");

            migrationBuilder.DropTable(
                name: "user_playlist_medias");

            migrationBuilder.DropTable(
                name: "podcasts");

            migrationBuilder.DropTable(
                name: "user_playlists");

            migrationBuilder.AddColumn<Guid>(
                name: "StationId",
                table: "media_files",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
