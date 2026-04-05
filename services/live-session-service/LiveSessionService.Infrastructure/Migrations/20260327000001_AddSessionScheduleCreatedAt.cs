using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSessionService.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSessionScheduleCreatedAt : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "created_at",
            table: "session_schedules",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "NOW()");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "created_at",
            table: "session_schedules");
    }
}
