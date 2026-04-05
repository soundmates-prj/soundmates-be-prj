using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountContentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTitleIntoProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'Email'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'email'
    ) THEN
        ALTER TABLE user_profile_read_models RENAME COLUMN ""Email"" TO email;
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'Id'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'id'
    ) THEN
        ALTER TABLE user_profile_read_models RENAME COLUMN ""Id"" TO id;
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'UpdatedAt'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'updated_at'
    ) THEN
        ALTER TABLE user_profile_read_models RENAME COLUMN ""UpdatedAt"" TO updated_at;
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'LastName'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'last_name'
    ) THEN
        ALTER TABLE user_profile_read_models RENAME COLUMN ""LastName"" TO last_name;
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'IsPending'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'is_pending'
    ) THEN
        ALTER TABLE user_profile_read_models RENAME COLUMN ""IsPending"" TO is_pending;
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'FullName'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'full_name'
    ) THEN
        ALTER TABLE user_profile_read_models RENAME COLUMN ""FullName"" TO full_name;
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'FirstName'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'first_name'
    ) THEN
        ALTER TABLE user_profile_read_models RENAME COLUMN ""FirstName"" TO first_name;
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'CreatedAt'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'created_at'
    ) THEN
        ALTER TABLE user_profile_read_models RENAME COLUMN ""CreatedAt"" TO created_at;
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'AvatarUrl'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_profile_read_models' AND column_name = 'avatar_url'
    ) THEN
        ALTER TABLE user_profile_read_models RENAME COLUMN ""AvatarUrl"" TO avatar_url;
    END IF;

    IF EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE schemaname = 'public' AND tablename = 'user_profile_read_models' AND indexname = 'IX_user_profile_read_models_Id'
    ) AND NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE schemaname = 'public' AND tablename = 'user_profile_read_models' AND indexname = 'IX_user_profile_read_models_id'
    ) THEN
        ALTER INDEX ""IX_user_profile_read_models_Id"" RENAME TO ""IX_user_profile_read_models_id"";
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
ALTER TABLE ""Notifications""
ADD COLUMN IF NOT EXISTS ""Title"" text NOT NULL DEFAULT '';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE ""Notifications"" DROP COLUMN IF EXISTS ""Title"";
");
        }
    }
}
