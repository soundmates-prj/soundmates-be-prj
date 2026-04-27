using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSessionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStationMediaFileMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create StationMediaFile mapping table
            migrationBuilder.CreateTable(
                name: "station_media_files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AzuraCastMediaId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_station_media_files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_station_media_files_media_files_MediaFileId",
                        column: x => x.MediaFileId,
                        principalTable: "media_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_station_media_files_azuracast_stations_StationId",
                        column: x => x.StationId,
                        principalTable: "azuracast_stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_station_media_files_MediaFileId_StationId",
                table: "station_media_files",
                columns: new[] { "MediaFileId", "StationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_station_media_files_StationId",
                table: "station_media_files",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_station_media_files_AzuraCastMediaId",
                table: "station_media_files",
                column: "AzuraCastMediaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "station_media_files");
        }
    }
}
