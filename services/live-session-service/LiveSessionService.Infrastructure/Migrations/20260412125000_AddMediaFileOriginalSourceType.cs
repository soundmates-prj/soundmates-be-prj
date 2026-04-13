using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveSessionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaFileOriginalSourceType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add OriginalSourceType column with default value "system"
            migrationBuilder.AddColumn<string>(
                name: "OriginalSourceType",
                table: "media_files",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "system");

            // Backfill existing records:
            // - If AzuraCastMediaId is not null → station (uploaded directly to AzuraCast)
            // - If AzuraCastMediaId is null → system (uploaded to Cloudinary)
            migrationBuilder.Sql(@"
                UPDATE ""media_files""
                SET ""OriginalSourceType"" = 
                    CASE 
                        WHEN ""AzuraCastMediaId"" IS NOT NULL AND ""AzuraCastMediaId"" != '' THEN 'station'
                        ELSE 'system'
                    END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalSourceType",
                table: "media_files");
        }
    }
}
