using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSessionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSavedPodcast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "session_schedules",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateTable(
                name: "user_saved_podcasts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    podcast_id = table.Column<Guid>(type: "uuid", nullable: false),
                    saved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_saved_podcasts", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_saved_podcasts_podcasts_podcast_id",
                        column: x => x.podcast_id,
                        principalTable: "podcasts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_saved_podcasts_podcast_id",
                table: "user_saved_podcasts",
                column: "podcast_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_saved_podcasts_user_id",
                table: "user_saved_podcasts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_saved_podcasts_user_id_podcast_id",
                table: "user_saved_podcasts",
                columns: new[] { "user_id", "podcast_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_saved_podcasts");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "session_schedules",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");
        }
    }
}
