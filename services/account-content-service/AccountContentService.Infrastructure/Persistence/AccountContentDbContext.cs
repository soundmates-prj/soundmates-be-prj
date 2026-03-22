using Microsoft.EntityFrameworkCore;
using AccountContentService.Domain.Entities;

public class AccountContentDbContext : DbContext
{
    public AccountContentDbContext(DbContextOptions<AccountContentDbContext> options)
       : base(options)
    {
    }

    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<BlogComment> BlogComments => Set<BlogComment>();
    public DbSet<PostReaction> PostReactions => Set<PostReaction>();
    public DbSet<ContentReport> ContentReports => Set<ContentReport>();
    public DbSet<Theme> Themes => Set<Theme>();

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PaymentWebhookLog> PaymentWebhookLogs => Set<PaymentWebhookLog>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    public DbSet<ServiceConfig> ServiceConfigs => Set<ServiceConfig>();

    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceConfig>(entity =>
        {
            entity.ToTable("ServiceConfigs");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Provider)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.ApiKey)
                .IsRequired();

            entity.Property(x => x.IsActive)
                .HasDefaultValue(true);

            entity.Property(x => x.CreatedAt)
                .HasColumnType("timestamp with time zone");

            entity.Property(x => x.UpdatedAt)
                .HasColumnType("timestamp with time zone");

            entity.HasIndex(x => new { x.Provider, x.IsActive });
        });

        modelBuilder.Entity<SystemConfig>(entity =>
        {
            entity.ToTable("SystemConfigs");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ConfigKey)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.ConfigValue)
                .IsRequired();

            entity.Property(x => x.Category)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.Property(x => x.IsEncrypted)
                .HasDefaultValue(false);

            entity.Property(x => x.IsSensitive)
                .HasDefaultValue(false);

            entity.Property(x => x.IsActive)
                .HasDefaultValue(true);

            entity.Property(x => x.CreatedAt)
                .HasColumnType("timestamp with time zone");

            entity.Property(x => x.UpdatedAt)
                .HasColumnType("timestamp with time zone");

            // Unique index on ConfigKey
            entity.HasIndex(x => x.ConfigKey)
                .IsUnique();

            // Index on Category for faster filtering
            entity.HasIndex(x => x.Category);

            // Index on IsActive for faster filtering
            entity.HasIndex(x => x.IsActive);
        });

        base.OnModelCreating(modelBuilder);
    }

}