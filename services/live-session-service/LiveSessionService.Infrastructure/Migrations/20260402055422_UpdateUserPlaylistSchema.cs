using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSessionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserPlaylistSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_playlists_external_playlist_id",
                table: "user_playlists");

            migrationBuilder.DropColumn(
                name: "external_playlist_id",
                table: "user_playlists");

            migrationBuilder.DropColumn(
                name: "include_in_on_demand",
                table: "user_playlists");

            migrationBuilder.DropColumn(
                name: "include_in_requests",
                table: "user_playlists");

            migrationBuilder.DropColumn(
                name: "last_synced_at",
                table: "user_playlists");

            migrationBuilder.DropColumn(
                name: "source",
                table: "user_playlists");

            migrationBuilder.DropColumn(
                name: "weight",
                table: "user_playlists");

            migrationBuilder.AlterColumn<int>(
                name: "playlist_order",
                table: "user_playlists",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "user_playlists",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_url",
                table: "user_playlists",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "visibility",
                table: "user_playlists",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_user_playlists_user_id_playlist_order",
                table: "user_playlists",
                columns: new[] { "user_id", "playlist_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_playlists_user_id_playlist_order",
                table: "user_playlists");

            migrationBuilder.DropColumn(
                name: "description",
                table: "user_playlists");

            migrationBuilder.DropColumn(
                name: "thumbnail_url",
                table: "user_playlists");

            migrationBuilder.DropColumn(
                name: "visibility",
                table: "user_playlists");

            migrationBuilder.AlterColumn<int>(
                name: "playlist_order",
                table: "user_playlists",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "external_playlist_id",
                table: "user_playlists",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "include_in_on_demand",
                table: "user_playlists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "include_in_requests",
                table: "user_playlists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_synced_at",
                table: "user_playlists",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "user_playlists",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "weight",
                table: "user_playlists",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_user_playlists_external_playlist_id",
                table: "user_playlists",
                column: "external_playlist_id");
        }
    }
}
