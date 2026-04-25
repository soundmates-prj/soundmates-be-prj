using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSessionService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPurchasedPodcasts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPurchasedPodcasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PodcastId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPurchasedPodcasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPurchasedPodcasts_podcasts_PodcastId",
                        column: x => x.PodcastId,
                        principalTable: "podcasts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPurchasedPodcasts_PodcastId",
                table: "UserPurchasedPodcasts",
                column: "PodcastId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPurchasedPodcasts_UserId_PodcastId",
                table: "UserPurchasedPodcasts",
                columns: new[] { "UserId", "PodcastId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPurchasedPodcasts");
        }
    }
}
