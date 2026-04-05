using AiService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace AiService.Infrastructure.Persistence;

public partial class AiDbContext : DbContext
{
    private readonly IConfiguration? _configuration;

    public AiDbContext()
    {
    }

    public AiDbContext(DbContextOptions<AiDbContext> options) : base(options)
    {
    }

    public AiDbContext(DbContextOptions<AiDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    public virtual DbSet<AiPrompt> AiPrompts { get; set; }
    public virtual DbSet<AiServiceConfig> AiServiceConfigs { get; set; }
    public virtual DbSet<Script> Scripts { get; set; }
    public virtual DbSet<TtsVoice> TtsVoices { get; set; }
    public virtual DbSet<ScriptAudio> ScriptAudios { get; set; }
    public virtual DbSet<AiUsage> AiUsages { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var config = _configuration ?? new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = ResolveConnectionString(config);
            optionsBuilder.UseNpgsql(connectionString);
        }
    }

    private static string ResolveConnectionString(IConfiguration config)
    {
        var raw = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        }

        return Regex.Replace(raw, "\\$\\{(?<key>[A-Za-z0-9_]+)\\}", match =>
        {
            var key = match.Groups["key"].Value;
            var value = Environment.GetEnvironmentVariable(key) ?? config[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Missing environment variable '{key}' required for connection string.");
            }

            return value;
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<AiPrompt>(entity =>
        {
            entity.HasKey(e => e.PromptId).HasName("ai_prompts_pkey");
            entity.ToTable("ai_prompts");

            entity.Property(e => e.PromptId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("prompt_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ContextType).HasMaxLength(50).HasColumnName("context_type");
            entity.Property(e => e.InputText).HasColumnName("input_text");
            entity.Property(e => e.ModelName).HasMaxLength(100).HasColumnName("model_name");
            entity.Property(e => e.Temperature).HasColumnType("numeric(3,2)").HasColumnName("temperature");
            entity.Property(e => e.MaxTokens).HasColumnName("max_tokens");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now() at time zone 'utc'")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
        });

        modelBuilder.Entity<AiServiceConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ai_service_configs_pkey");
            entity.ToTable("ai_service_configs");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Provider)
                .HasMaxLength(50)
                .HasColumnName("provider");
            entity.Property(e => e.ApiKey)
                .HasColumnName("api_key");
            entity.Property(e => e.PromptTemplate)
                .HasColumnName("prompt_template");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now() at time zone 'utc'")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            entity.HasIndex(e => new { e.Provider, e.IsActive }, "ai_service_configs_provider_active_idx");
        });

        modelBuilder.Entity<Script>(entity =>
        {
            entity.HasKey(e => e.ScriptId).HasName("scripts_pkey");
            entity.ToTable("scripts");

            entity.Property(e => e.ScriptId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("script_id");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.ScriptSource).HasMaxLength(50).HasColumnName("script_source");
            entity.Property(e => e.ContextType).HasMaxLength(50).HasColumnName("context_type");
            entity.Property(e => e.Title).HasMaxLength(255).HasColumnName("title");
            entity.Property(e => e.ContentText).HasColumnName("content_text");
            entity.Property(e => e.Status).HasMaxLength(20).HasColumnName("status");

            entity.Property(e => e.PromptId).HasColumnName("prompt_id");
            entity.HasOne(d => d.Prompt).WithMany(p => p.Scripts)
                .HasForeignKey(d => d.PromptId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("scripts_prompt_id_fkey");

            entity.Property(e => e.ParentScriptId).HasColumnName("parent_script_id");
            entity.HasOne(d => d.ParentScript).WithMany(p => p.ChildScripts)
                .HasForeignKey(d => d.ParentScriptId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("scripts_parent_script_id_fkey");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now() at time zone 'utc'")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            entity.HasIndex(e => e.AuthorId, "scripts_author_id_idx");
            entity.HasIndex(e => new { e.AuthorId, e.ContextType }, "scripts_author_context_idx");
        });

        modelBuilder.Entity<TtsVoice>(entity =>
        {
            entity.HasKey(e => e.VoiceId).HasName("tts_voices_pkey");
            entity.ToTable("tts_voices");

            entity.Property(e => e.VoiceId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("voice_id");

            entity.Property(e => e.Provider).HasMaxLength(50).HasColumnName("provider");
            entity.Property(e => e.VoiceCode).HasMaxLength(100).HasColumnName("voice_code");
            entity.Property(e => e.DisplayName).HasMaxLength(100).HasColumnName("display_name");
            entity.Property(e => e.Region).HasMaxLength(50).HasColumnName("region");
            entity.Property(e => e.Gender).HasMaxLength(20).HasColumnName("gender");
            entity.Property(e => e.Model).HasMaxLength(100).HasColumnName("model");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now() at time zone 'utc'")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_tts_voices_user_id");
            entity.HasIndex(e => new { e.Provider, e.VoiceCode }, "tts_voices_provider_voice_code_key").IsUnique();
        });

        modelBuilder.Entity<ScriptAudio>(entity =>
        {
            entity.HasKey(e => e.AudioId).HasName("script_audios_pkey");
            entity.ToTable("script_audios");

            entity.Property(e => e.AudioId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("audio_id");

            entity.Property(e => e.AudioUrl).HasColumnName("audio_url");
            entity.Property(e => e.AudioPath).HasColumnName("audio_path");
            entity.Property(e => e.Speed).HasColumnType("numeric(3,2)").HasColumnName("speed");
            entity.Property(e => e.Pitch).HasColumnType("numeric(3,2)").HasColumnName("pitch");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.Status).HasMaxLength(20).HasColumnName("status");

            entity.Property(e => e.ScriptId).HasColumnName("script_id");
            entity.HasOne(d => d.Script).WithMany(p => p.Audios)
                .HasForeignKey(d => d.ScriptId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("script_audios_script_id_fkey");

            entity.Property(e => e.VoiceId).HasColumnName("voice_id");
            entity.HasOne(d => d.Voice).WithMany(p => p.Audios)
                .HasForeignKey(d => d.VoiceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("script_audios_voice_id_fkey");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now() at time zone 'utc'")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            entity.HasIndex(e => e.ScriptId, "script_audios_script_id_idx");
        });

        modelBuilder.Entity<AiUsage>(entity =>
        {
            entity.HasKey(e => e.UsageId).HasName("ai_usage_pkey");
            entity.ToTable("ai_usage");

            entity.Property(e => e.UsageId)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("usage_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Provider).HasMaxLength(50).HasColumnName("provider");
            entity.Property(e => e.TokensUsed).HasColumnName("tokens_used");
            entity.Property(e => e.Cost).HasColumnType("numeric(10,4)").HasColumnName("cost");
            entity.Property(e => e.ScriptId).HasColumnName("script_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now() at time zone 'utc'")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Script).WithMany()
                .HasForeignKey(d => d.ScriptId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("ai_usage_script_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

