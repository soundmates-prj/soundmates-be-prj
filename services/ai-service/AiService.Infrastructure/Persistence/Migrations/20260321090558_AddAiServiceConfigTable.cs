using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiServiceConfigTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_service_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    api_key = table.Column<string>(type: "text", nullable: false),
                    prompt_template = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ai_service_configs_pkey", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ai_service_configs_provider_active_idx",
                table: "ai_service_configs",
                columns: new[] { "provider", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_service_configs");
        }
    }
}
