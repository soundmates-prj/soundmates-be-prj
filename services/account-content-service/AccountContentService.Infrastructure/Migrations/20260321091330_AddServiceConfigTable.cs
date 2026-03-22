using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountContentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceConfigTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ApiKey = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceConfigs_Provider_IsActive",
                table: "ServiceConfigs",
                columns: new[] { "Provider", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceConfigs");
        }
    }
}
