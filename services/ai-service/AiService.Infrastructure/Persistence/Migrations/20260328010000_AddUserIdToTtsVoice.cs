using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToTtsVoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "tts_voices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tts_voices_user_id",
                table: "tts_voices",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tts_voices_user_id",
                table: "tts_voices");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "tts_voices");
        }
    }
}
