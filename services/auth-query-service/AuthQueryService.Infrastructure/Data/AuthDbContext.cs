using AuthQueryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthQueryService.Infrastructure.Data
{
    /// <summary>
    /// AuthDbContext for Query Service - minimal implementation
    /// This service is primarily for reading from MongoDB
    /// This DbContext is only needed for Outbox pattern if write operations are added later
    /// </summary>
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // OutboxMessage configuration
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.ToTable("outbox_messages");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(256);
                entity.Property(e => e.Payload).IsRequired();
                entity.HasIndex(e => e.ProcessedOnUtc);
            });

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(128);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // UserRole configuration
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("user_roles");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(128);
            });
        }
    }
}

