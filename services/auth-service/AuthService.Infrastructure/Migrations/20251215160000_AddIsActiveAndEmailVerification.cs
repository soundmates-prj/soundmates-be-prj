using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace AuthService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveAndEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "email_verification_token",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "email_verified_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            // Update existing users to be active (for backward compatibility)
            migrationBuilder.Sql("UPDATE users SET is_active = true WHERE email_verification_token IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_verification_token",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_verified_at",
                table: "users");
        }
    }
}

