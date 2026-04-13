using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSessionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToStationPlaylist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add soft delete columns to StationPlaylists table
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StationPlaylists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StationPlaylists",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "StationPlaylists",
                type: "uuid",
                nullable: true);

            // Create indexes for better query performance
            migrationBuilder.CreateIndex(
                name: "IX_StationPlaylists_IsDeleted",
                table: "StationPlaylists",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StationPlaylists_DeletedAt",
                table: "StationPlaylists",
                column: "DeletedAt")
                .Annotation("Npgsql:IndexInclude", new[] { "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_StationPlaylists_AzuraCastStationId_IsDeleted",
                table: "StationPlaylists",
                columns: new[] { "AzuraCastStationId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_StationPlaylists_AzuraCastStationId_IsDeleted",
                table: "StationPlaylists");

            migrationBuilder.DropIndex(
                name: "IX_StationPlaylists_DeletedAt",
                table: "StationPlaylists");

            migrationBuilder.DropIndex(
                name: "IX_StationPlaylists_IsDeleted",
                table: "StationPlaylists");

            // Drop columns
            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "StationPlaylists");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StationPlaylists");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StationPlaylists");
        }
    }
}
