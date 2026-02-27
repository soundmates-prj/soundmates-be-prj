using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSessionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "azuracast_stations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_station_id = table.Column<int>(type: "integer", nullable: false),
                    station_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    station_shortcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    stream_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    public_player_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    mount_point = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    api_base_url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    api_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sync_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_sync_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_azuracast_stations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ListenerStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LiveSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentListeners = table.Column<int>(type: "integer", nullable: false),
                    UniqueListeners = table.Column<int>(type: "integer", nullable: false),
                    PeakListeners = table.Column<int>(type: "integer", nullable: false),
                    TotalConnections = table.Column<int>(type: "integer", nullable: false),
                    AverageListenTimeSeconds = table.Column<double>(type: "double precision", nullable: false),
                    TopCountry = table.Column<string>(type: "text", nullable: true),
                    TopCity = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListenerStatistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "live_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    azuracast_station_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    max_listeners = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    thumbnail_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    genre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_live_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_live_sessions_azuracast_stations_azuracast_station_id",
                        column: x => x.azuracast_station_id,
                        principalTable: "azuracast_stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StationMounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AzuraCastStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalMountId = table.Column<int>(type: "integer", nullable: false),
                    MountName = table.Column<string>(type: "text", nullable: false),
                    MountPath = table.Column<string>(type: "text", nullable: false),
                    MountUrl = table.Column<string>(type: "text", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    Bitrate = table.Column<int>(type: "integer", nullable: true),
                    Format = table.Column<string>(type: "text", nullable: true),
                    CurrentListeners = table.Column<int>(type: "integer", nullable: true),
                    UniqueListeners = table.Column<int>(type: "integer", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationMounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StationMounts_azuracast_stations_AzuraCastStationId",
                        column: x => x.AzuraCastStationId,
                        principalTable: "azuracast_stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StationPlaylists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AzuraCastStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalPlaylistId = table.Column<int>(type: "integer", nullable: false),
                    PlaylistName = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    PlaylistOrder = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IncludeInRequests = table.Column<bool>(type: "boolean", nullable: false),
                    IncludeInOnDemand = table.Column<bool>(type: "boolean", nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationPlaylists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StationPlaylists_azuracast_stations_AzuraCastStationId",
                        column: x => x.AzuraCastStationId,
                        principalTable: "azuracast_stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "now_playing_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    live_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    azuracast_song_history_id = table.Column<long>(type: "bigint", nullable: true),
                    song_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    song_artist = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    song_album = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    song_art_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    played_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    listener_peak = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    listener_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_request = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_now_playing_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_now_playing_history_live_sessions_live_session_id",
                        column: x => x.live_session_id,
                        principalTable: "live_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LiveSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivityType = table.Column<int>(type: "integer", nullable: false),
                    ActivityData = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionActivities_live_sessions_LiveSessionId",
                        column: x => x.LiveSessionId,
                        principalTable: "live_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionListeners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LiveSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousIdentifier = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    ConnectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DisconnectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsConnected = table.Column<bool>(type: "boolean", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionListeners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionListeners_live_sessions_LiveSessionId",
                        column: x => x.LiveSessionId,
                        principalTable: "live_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LiveSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CanManagePlaylist = table.Column<bool>(type: "boolean", nullable: false),
                    CanModerate = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionParticipants_live_sessions_LiveSessionId",
                        column: x => x.LiveSessionId,
                        principalTable: "live_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaylistMedias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StationPlaylistId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaId = table.Column<string>(type: "text", nullable: false),
                    SongTitle = table.Column<string>(type: "text", nullable: false),
                    SongArtist = table.Column<string>(type: "text", nullable: true),
                    SongAlbum = table.Column<string>(type: "text", nullable: true),
                    SongArtUrl = table.Column<string>(type: "text", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: true),
                    PlayCount = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistMedias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaylistMedias_StationPlaylists_StationPlaylistId",
                        column: x => x.StationPlaylistId,
                        principalTable: "StationPlaylists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_azuracast_stations_external_station_id",
                table: "azuracast_stations",
                column: "external_station_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_azuracast_stations_is_enabled",
                table: "azuracast_stations",
                column: "is_enabled");

            migrationBuilder.CreateIndex(
                name: "IX_azuracast_stations_sync_status",
                table: "azuracast_stations",
                column: "sync_status");

            migrationBuilder.CreateIndex(
                name: "IX_live_sessions_azuracast_station_id",
                table: "live_sessions",
                column: "azuracast_station_id");

            migrationBuilder.CreateIndex(
                name: "IX_live_sessions_host_user_id",
                table: "live_sessions",
                column: "host_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_live_sessions_is_public",
                table: "live_sessions",
                column: "is_public");

            migrationBuilder.CreateIndex(
                name: "IX_live_sessions_started_at",
                table: "live_sessions",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "IX_live_sessions_status",
                table: "live_sessions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_now_playing_history_live_session_id",
                table: "now_playing_history",
                column: "live_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_now_playing_history_live_session_id_played_at",
                table: "now_playing_history",
                columns: new[] { "live_session_id", "played_at" });

            migrationBuilder.CreateIndex(
                name: "IX_now_playing_history_played_at",
                table: "now_playing_history",
                column: "played_at");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_occurred_on_utc",
                table: "outbox_messages",
                column: "occurred_on_utc");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_on_utc",
                table: "outbox_messages",
                column: "processed_on_utc");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_on_utc_retry_count",
                table: "outbox_messages",
                columns: new[] { "processed_on_utc", "retry_count" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistMedias_StationPlaylistId",
                table: "PlaylistMedias",
                column: "StationPlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionActivities_LiveSessionId",
                table: "SessionActivities",
                column: "LiveSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionListeners_LiveSessionId",
                table: "SessionListeners",
                column: "LiveSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionParticipants_LiveSessionId",
                table: "SessionParticipants",
                column: "LiveSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StationMounts_AzuraCastStationId",
                table: "StationMounts",
                column: "AzuraCastStationId");

            migrationBuilder.CreateIndex(
                name: "IX_StationPlaylists_AzuraCastStationId",
                table: "StationPlaylists",
                column: "AzuraCastStationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListenerStatistics");

            migrationBuilder.DropTable(
                name: "now_playing_history");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "PlaylistMedias");

            migrationBuilder.DropTable(
                name: "SessionActivities");

            migrationBuilder.DropTable(
                name: "SessionListeners");

            migrationBuilder.DropTable(
                name: "SessionParticipants");

            migrationBuilder.DropTable(
                name: "StationMounts");

            migrationBuilder.DropTable(
                name: "StationPlaylists");

            migrationBuilder.DropTable(
                name: "live_sessions");

            migrationBuilder.DropTable(
                name: "azuracast_stations");
        }
    }
}
