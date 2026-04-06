using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountContentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVoiceCloneFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm các cột mới vào SubscriptionPlans
            migrationBuilder.AddColumn<int>(
                name: "PodcastRequestLimit",
                table: "SubscriptionPlans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TtsMinuteLimit",
                table: "SubscriptionPlans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VoiceModelLimit",
                table: "SubscriptionPlans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Tạo bảng UserVoiceModels
            migrationBuilder.CreateTable(
                name: "UserVoiceModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VoiceCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceAudioDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    SourceAudioUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVoiceModels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserVoiceModels_Status",
                table: "UserVoiceModels",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserVoiceModels_UserId",
                table: "UserVoiceModels",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserVoiceModels");

            migrationBuilder.DropColumn(
                name: "PodcastRequestLimit",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "TtsMinuteLimit",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "VoiceModelLimit",
                table: "SubscriptionPlans");
        }
    }
}
