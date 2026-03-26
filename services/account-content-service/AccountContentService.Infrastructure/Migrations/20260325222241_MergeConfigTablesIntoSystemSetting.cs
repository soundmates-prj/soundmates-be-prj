using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountContentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MergeConfigTablesIntoSystemSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"ServiceConfigs\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"SystemConfigs\";");

            // Rename Descreption -> Description (skip if already renamed)
            migrationBuilder.Sql(
                @"ALTER TABLE ""SystemSettings"" RENAME COLUMN ""Descreption"" TO ""Description"";");

            // Add missing columns idempotently
            migrationBuilder.Sql(
                @"ALTER TABLE ""SystemSettings"" ADD COLUMN IF NOT EXISTS ""Category"" text NOT NULL DEFAULT '';");
            migrationBuilder.Sql(
                @"ALTER TABLE ""SystemSettings"" ADD COLUMN IF NOT EXISTS ""IsEncrypted"" boolean NOT NULL DEFAULT false;");
            migrationBuilder.Sql(
                @"ALTER TABLE ""SystemSettings"" ADD COLUMN IF NOT EXISTS ""IsSensitive"" boolean NOT NULL DEFAULT false;");
            migrationBuilder.Sql(
                @"ALTER TABLE ""SystemSettings"" ADD COLUMN IF NOT EXISTS ""Provider"" text;");
            migrationBuilder.Sql(
                @"ALTER TABLE ""SystemSettings"" ADD COLUMN IF NOT EXISTS ""UpdatedByUserId"" uuid;");
            migrationBuilder.Sql(
                @"ALTER TABLE ""SystemSettings"" ADD COLUMN IF NOT EXISTS ""IsActive"" boolean NOT NULL DEFAULT true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "IsEncrypted",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "IsSensitive",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "SystemSettings");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "SystemSettings",
                newName: "Descreption");

            migrationBuilder.CreateTable(
                name: "ServiceConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiKey = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConfigKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConfigValue = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsEncrypted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsSensitive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceConfigs_Provider_IsActive",
                table: "ServiceConfigs",
                columns: new[] { "Provider", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigs_Category",
                table: "SystemConfigs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigs_ConfigKey",
                table: "SystemConfigs",
                column: "ConfigKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigs_IsActive",
                table: "SystemConfigs",
                column: "IsActive");
        }
    }
}
