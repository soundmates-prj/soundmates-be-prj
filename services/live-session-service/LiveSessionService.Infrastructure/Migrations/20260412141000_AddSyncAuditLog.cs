using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSessionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sync_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sync_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: true),
                    triggered_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_records = table.Column<int>(type: "integer", nullable: false),
                    created_records = table.Column<int>(type: "integer", nullable: false),
                    updated_records = table.Column<int>(type: "integer", nullable: false),
                    deleted_records = table.Column<int>(type: "integer", nullable: false),
                    failed_records = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    error_details = table.Column<string>(type: "jsonb", nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_audit_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sync_audit_logs_started_at",
                table: "sync_audit_logs",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "ix_sync_audit_logs_station_id",
                table: "sync_audit_logs",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "ix_sync_audit_logs_status",
                table: "sync_audit_logs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_sync_audit_logs_sync_type",
                table: "sync_audit_logs",
                column: "sync_type");

            migrationBuilder.CreateIndex(
                name: "ix_sync_audit_logs_triggered_by_user_id",
                table: "sync_audit_logs",
                column: "triggered_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sync_audit_logs");
        }
    }
}
