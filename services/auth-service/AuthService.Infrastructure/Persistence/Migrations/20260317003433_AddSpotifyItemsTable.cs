using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpotifyItemsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "spotify_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    spotify_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    item_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    artist_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    album_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    img_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    preview_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    raw_json = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("spotify_items_pkey", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "spotify_items_spotify_id_item_type_key",
                table: "spotify_items",
                columns: new[] { "spotify_id", "item_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "spotify_items");
        }
    }
}
