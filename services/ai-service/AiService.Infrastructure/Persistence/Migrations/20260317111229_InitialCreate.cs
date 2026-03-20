using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "ai_prompts",
                columns: table => new
                {
                    prompt_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    context_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    input_text = table.Column<string>(type: "text", nullable: false),
                    model_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    temperature = table.Column<decimal>(type: "numeric(3,2)", nullable: true),
                    max_tokens = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("ai_prompts_pkey", x => x.prompt_id);
                });

            migrationBuilder.CreateTable(
                name: "tts_voices",
                columns: table => new
                {
                    voice_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    voice_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    region = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("tts_voices_pkey", x => x.voice_id);
                });

            migrationBuilder.CreateTable(
                name: "scripts",
                columns: table => new
                {
                    script_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    script_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    context_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    content_text = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    prompt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_script_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("scripts_pkey", x => x.script_id);
                    table.ForeignKey(
                        name: "scripts_parent_script_id_fkey",
                        column: x => x.parent_script_id,
                        principalTable: "scripts",
                        principalColumn: "script_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "scripts_prompt_id_fkey",
                        column: x => x.prompt_id,
                        principalTable: "ai_prompts",
                        principalColumn: "prompt_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ai_usage",
                columns: table => new
                {
                    usage_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tokens_used = table.Column<int>(type: "integer", nullable: true),
                    cost = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    script_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("ai_usage_pkey", x => x.usage_id);
                    table.ForeignKey(
                        name: "ai_usage_script_id_fkey",
                        column: x => x.script_id,
                        principalTable: "scripts",
                        principalColumn: "script_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "script_audios",
                columns: table => new
                {
                    audio_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    audio_url = table.Column<string>(type: "text", nullable: false),
                    audio_path = table.Column<string>(type: "text", nullable: false),
                    speed = table.Column<decimal>(type: "numeric(3,2)", nullable: true),
                    pitch = table.Column<decimal>(type: "numeric(3,2)", nullable: true),
                    duration = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    script_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("script_audios_pkey", x => x.audio_id);
                    table.ForeignKey(
                        name: "script_audios_script_id_fkey",
                        column: x => x.script_id,
                        principalTable: "scripts",
                        principalColumn: "script_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "script_audios_voice_id_fkey",
                        column: x => x.voice_id,
                        principalTable: "tts_voices",
                        principalColumn: "voice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_script_id",
                table: "ai_usage",
                column: "script_id");

            migrationBuilder.CreateIndex(
                name: "IX_script_audios_voice_id",
                table: "script_audios",
                column: "voice_id");

            migrationBuilder.CreateIndex(
                name: "script_audios_script_id_idx",
                table: "script_audios",
                column: "script_id");

            migrationBuilder.CreateIndex(
                name: "IX_scripts_parent_script_id",
                table: "scripts",
                column: "parent_script_id");

            migrationBuilder.CreateIndex(
                name: "IX_scripts_prompt_id",
                table: "scripts",
                column: "prompt_id");

            migrationBuilder.CreateIndex(
                name: "scripts_author_context_idx",
                table: "scripts",
                columns: new[] { "author_id", "context_type" });

            migrationBuilder.CreateIndex(
                name: "scripts_author_id_idx",
                table: "scripts",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "tts_voices_provider_voice_code_key",
                table: "tts_voices",
                columns: new[] { "provider", "voice_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_usage");

            migrationBuilder.DropTable(
                name: "script_audios");

            migrationBuilder.DropTable(
                name: "scripts");

            migrationBuilder.DropTable(
                name: "tts_voices");

            migrationBuilder.DropTable(
                name: "ai_prompts");
        }
    }
}
