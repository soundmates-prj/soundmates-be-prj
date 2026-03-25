using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountContentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileIntoPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ThemeName",
                table: "Themes",
                newName: "name");

            migrationBuilder.Sql(@"
UPDATE ""Themes""
SET ""name"" = LEFT(""name"", 150)
WHERE LENGTH(""name"") > 150;
");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Themes",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SecondaryColor",
                table: "Themes",
                newName: "secondary_color");

            migrationBuilder.RenameColumn(
                name: "PrimaryColor",
                table: "Themes",
                newName: "primary_color");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Themes",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "FontFamily",
                table: "Themes",
                newName: "font_family");

            migrationBuilder.RenameColumn(
                name: "CustomCss",
                table: "Themes",
                newName: "gradient_background");

            migrationBuilder.AlterColumn<string>(
                name: "secondary_color",
                table: "Themes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "primary_color",
                table: "Themes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "font_family",
                table: "Themes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "background_color",
                table: "Themes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<JsonElement>(
                name: "config_json",
                table: "Themes",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "Themes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "mode",
                table: "Themes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "light");

            migrationBuilder.AddColumn<string>(
                name: "mood",
                table: "Themes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "Themes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "player_color",
                table: "Themes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "text_color",
                table: "Themes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Themes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UserAvatarUrl",
                table: "BlogPosts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserFullName",
                table: "BlogPosts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
UPDATE ""Themes""
SET ""name"" = CASE
    WHEN ""name"" IS NULL OR ""name"" = '' THEN 'theme-' || ""id""::text
    ELSE ""name""
END;
");

            migrationBuilder.Sql(@"
WITH ranked AS (
    SELECT ""id"", ""name"",
           ROW_NUMBER() OVER (PARTITION BY ""name"" ORDER BY ""id"") AS rn
    FROM ""Themes""
)
UPDATE ""Themes"" t
SET ""name"" = LEFT(t.""name"", 140) || '-' || ranked.rn::text
FROM ranked
WHERE t.""id"" = ranked.""id"" AND ranked.rn > 1;
");

            migrationBuilder.CreateIndex(
                name: "IX_Themes_name",
                table: "Themes",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Themes_name",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "background_color",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "config_json",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "mode",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "mood",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "player_color",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "text_color",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "UserAvatarUrl",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "UserFullName",
                table: "BlogPosts");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Themes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "secondary_color",
                table: "Themes",
                newName: "SecondaryColor");

            migrationBuilder.RenameColumn(
                name: "primary_color",
                table: "Themes",
                newName: "PrimaryColor");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Themes",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "font_family",
                table: "Themes",
                newName: "FontFamily");

            migrationBuilder.RenameColumn(
                name: "gradient_background",
                table: "Themes",
                newName: "CustomCss");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "Themes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Themes",
                newName: "ThemeName");

            migrationBuilder.AlterColumn<string>(
                name: "SecondaryColor",
                table: "Themes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PrimaryColor",
                table: "Themes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FontFamily",
                table: "Themes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThemeName",
                table: "Themes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
