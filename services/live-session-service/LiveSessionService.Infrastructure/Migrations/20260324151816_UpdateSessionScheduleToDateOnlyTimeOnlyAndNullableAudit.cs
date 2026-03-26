using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSessionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSessionScheduleToDateOnlyTimeOnlyAndNullableAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_session_schedules_start_time",
                table: "session_schedules");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "session_schedules",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "start_time",
                table: "session_schedules",
                type: "time",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "end_time",
                table: "session_schedules",
                type: "time",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "session_schedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "days_of_week",
                table: "session_schedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "end_date",
                table: "session_schedules",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_recurring",
                table: "session_schedules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "start_date",
                table: "session_schedules",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "session_schedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_session_schedules_start_date",
                table: "session_schedules",
                column: "start_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_session_schedules_start_date",
                table: "session_schedules");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "session_schedules");

            migrationBuilder.DropColumn(
                name: "days_of_week",
                table: "session_schedules");

            migrationBuilder.DropColumn(
                name: "end_date",
                table: "session_schedules");

            migrationBuilder.DropColumn(
                name: "is_recurring",
                table: "session_schedules");

            migrationBuilder.DropColumn(
                name: "start_date",
                table: "session_schedules");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "session_schedules");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "session_schedules",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_time",
                table: "session_schedules",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time");

            migrationBuilder.AlterColumn<DateTime>(
                name: "end_time",
                table: "session_schedules",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time");

            migrationBuilder.CreateIndex(
                name: "IX_session_schedules_start_time",
                table: "session_schedules",
                column: "start_time");
        }
    }
}
