using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitFullNameToFirstNameLastName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns
            migrationBuilder.AddColumn<string>(
                name: "first_name",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                table: "users",
                type: "text",
                nullable: true);

            // Migrate existing data: split full_name into first_name and last_name
            migrationBuilder.Sql(@"
                UPDATE users
                SET 
                    first_name = CASE 
                        WHEN full_name IS NULL OR full_name = '' THEN NULL
                        WHEN position(' ' in full_name) > 0 THEN substring(full_name, 1, position(' ' in full_name) - 1)
                        ELSE full_name
                    END,
                    last_name = CASE 
                        WHEN full_name IS NULL OR full_name = '' THEN NULL
                        WHEN position(' ' in full_name) > 0 THEN substring(full_name, position(' ' in full_name) + 1)
                        ELSE NULL
                    END
                WHERE full_name IS NOT NULL;
            ");

            // Drop the old full_name column
            migrationBuilder.DropColumn(
                name: "full_name",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back full_name column
            migrationBuilder.AddColumn<string>(
                name: "full_name",
                table: "users",
                type: "text",
                nullable: true);

            // Combine first_name and last_name back into full_name
            migrationBuilder.Sql(@"
                UPDATE users
                SET full_name = CASE
                    WHEN first_name IS NULL AND last_name IS NULL THEN NULL
                    WHEN first_name IS NULL THEN last_name
                    WHEN last_name IS NULL THEN first_name
                    ELSE first_name || ' ' || last_name
                END;
            ");

            // Drop new columns
            migrationBuilder.DropColumn(
                name: "first_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_name",
                table: "users");
        }
    }
}

