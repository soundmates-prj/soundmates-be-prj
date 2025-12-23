using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.RegularExpressions;

namespace AuthService.Infrastructure.Data
{
    public partial class AuthDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public AuthDbContext()
        {
        }

        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public AuthDbContext(DbContextOptions<AuthDbContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }

        public virtual DbSet<Oauthaccount> Oauthaccounts { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<UserRole> UserRoles { get; set; }
        public virtual DbSet<OutboxMessage> OutboxMessages { get; set; }
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
        public virtual DbSet<OtpCode> OtpCodes { get; set; }
        public virtual DbSet<Profile> Profiles { get; set; }

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
                optionsBuilder.UseNpgsql(connectionString)
                    .ConfigureWarnings(warnings => 
                        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
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

            modelBuilder.Entity<Oauthaccount>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("oauthaccount_pkey");
                entity.ToTable("oauthaccount");

                entity.Property(e => e.Id)
                    .HasDefaultValueSql("uuid_generate_v4()")
                    .HasColumnName("id");
                entity.Property(e => e.Provider).HasColumnName("provider");
                entity.Property(e => e.ProviderAccountId).HasColumnName("provider_account_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.User).WithMany(p => p.Oauthaccounts)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("oauthaccount_user_id_fkey");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("users_pkey");
                entity.ToTable("users");

                entity.HasIndex(e => e.Email, "users_email_key").IsUnique();
                entity.HasIndex(e => e.Username, "users_username_key").IsUnique();

                entity.Property(e => e.Id)
                    .HasDefaultValueSql("uuid_generate_v4()")
                    .HasColumnName("id");
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("now() at time zone 'utc'")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("updated_at");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.FirstName).HasColumnName("first_name");
                entity.Property(e => e.LastName).HasColumnName("last_name");
                entity.Property(e => e.Password).HasColumnName("password");
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.Username).HasColumnName("username");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(false)
                    .HasColumnName("is_active");
                entity.Property(e => e.EmailVerificationToken).HasColumnName("email_verification_token");
                entity.Property(e => e.EmailVerifiedAt)
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("email_verified_at");

                entity.HasOne(d => d.Role).WithMany(p => p.Users)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("users_role_id_fkey");
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("user_roles_pkey");
                entity.ToTable("user_roles");

                entity.HasIndex(e => e.Name, "user_roles_name_key").IsUnique();

                entity.Property(e => e.Id)
                    .HasDefaultValueSql("uuid_generate_v4()")
                    .HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name");
            });

            // Outbox table mapping
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("outbox_messages_pkey");
                entity.ToTable("outbox_messages");

                entity.Property(e => e.Id)
                    .HasDefaultValueSql("uuid_generate_v4()")
                    .HasColumnName("id");

                entity.Property(e => e.Type)
                    .HasMaxLength(255)
                    .HasColumnName("type");

                entity.Property(e => e.Payload)
                    .HasColumnName("payload");

                entity.Property(e => e.OccurredOnUtc)
                    .HasDefaultValueSql("now() at time zone 'utc'")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("occurred_on_utc");

                entity.Property(e => e.ProcessedOnUtc)
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("processed_on_utc");

                entity.Property(e => e.Error)
                    .HasColumnName("error");
            });

            // RefreshToken table mapping
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");
                entity.ToTable("refresh_tokens");

                entity.HasIndex(e => e.Token, "refresh_tokens_token_key").IsUnique();
                entity.HasIndex(e => e.UserId, "refresh_tokens_user_id_idx");

                entity.Property(e => e.Id)
                    .HasDefaultValueSql("uuid_generate_v4()")
                    .HasColumnName("id");

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id");

                entity.Property(e => e.Token)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("token");

                entity.Property(e => e.ExpiresAt)
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("expires_at");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("now() at time zone 'utc'")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_at");

                entity.Property(e => e.IsRevoked)
                    .HasDefaultValue(false)
                    .HasColumnName("is_revoked");

                entity.Property(e => e.RevokedAt)
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("revoked_at");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.RefreshTokens)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("refresh_tokens_user_id_fkey");
            });

            // OtpCode table mapping
            modelBuilder.Entity<OtpCode>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("otp_codes_pkey");
                entity.ToTable("otp_codes");

                entity.HasIndex(e => new { e.Email, e.Code, e.Purpose }, "otp_codes_email_code_purpose_idx");

                entity.Property(e => e.Id)
                    .HasDefaultValueSql("uuid_generate_v4()")
                    .HasColumnName("id");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("email");

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(10)
                    .HasColumnName("code");

                entity.Property(e => e.ExpiresAt)
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("expires_at");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("now() at time zone 'utc'")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_at");

                entity.Property(e => e.IsUsed)
                    .HasDefaultValue(false)
                    .HasColumnName("is_used");

                entity.Property(e => e.UsedAt)
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("used_at");

                entity.Property(e => e.Purpose)
                    .HasColumnName("purpose");
            });

            // Profile table mapping
            modelBuilder.Entity<Profile>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("profiles_pkey");
                entity.ToTable("profiles");

                entity.HasIndex(e => e.UserId, "profiles_user_id_key").IsUnique();

                entity.Property(e => e.Id)
                    .HasDefaultValueSql("uuid_generate_v4()")
                    .HasColumnName("id");

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id");

                entity.Property(e => e.Bio)
                    .HasMaxLength(500)
                    .HasColumnName("bio");

                entity.Property(e => e.ProfileImageUrl)
                    .HasMaxLength(500)
                    .HasColumnName("profile_image_url");

                entity.Property(e => e.BackgroundImageUrl)
                    .HasMaxLength(500)
                    .HasColumnName("background_image_url");

                entity.Property(e => e.Phone)
                    .HasMaxLength(20)
                    .HasColumnName("phone");

                entity.Property(e => e.Gender)
                    .HasMaxLength(20)
                    .HasColumnName("gender");

                entity.Property(e => e.DateOfBirth)
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("date_of_birth");

                entity.Property(e => e.Location)
                    .HasMaxLength(200)
                    .HasColumnName("location");

                entity.Property(e => e.Website)
                    .HasMaxLength(200)
                    .HasColumnName("website");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("now() at time zone 'utc'")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_at");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("updated_at");

                entity.HasOne(d => d.User)
                    .WithOne(p => p.Profile)
                    .HasForeignKey<Profile>(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("profiles_user_id_fkey");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
