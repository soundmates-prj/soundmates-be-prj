using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSessionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaFileAndMountSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MediaFileId",
                table: "PlaylistMedias",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "media_files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Artist = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Album = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Genre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ArtUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FileType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_files", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistMedias_MediaFileId",
                table: "PlaylistMedias",
                column: "MediaFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistMedias_media_files_MediaFileId",
                table: "PlaylistMedias",
                column: "MediaFileId",
                principalTable: "media_files",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistMedias_media_files_MediaFileId",
                table: "PlaylistMedias");

            migrationBuilder.DropTable(
                name: "media_files");

            migrationBuilder.DropIndex(
                name: "IX_PlaylistMedias_MediaFileId",
                table: "PlaylistMedias");

            migrationBuilder.DropColumn(
                name: "MediaFileId",
                table: "PlaylistMedias");
        }
    }
}
