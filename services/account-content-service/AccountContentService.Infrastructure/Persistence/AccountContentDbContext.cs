using AccountContentService.Domain.Entities;
using AccountContentService.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.RegularExpressions;

public class AccountContentDbContext : DbContext
{
    private readonly IConfiguration? _configuration;

    public AccountContentDbContext()
    {
    }

    public AccountContentDbContext(DbContextOptions<AccountContentDbContext> options)
       : base(options)
    {
    }

    public AccountContentDbContext(DbContextOptions<AccountContentDbContext> options, IConfiguration configuration)
       : base(options)
    {
        _configuration = configuration;
    }

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
    public DbSet<UserProfileReadModel> UserProfileReadModels => Set<UserProfileReadModel>();

    public DbSet<UserVoiceModel> UserVoiceModels => Set<UserVoiceModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ThemeConfiguration());
        modelBuilder.ApplyConfiguration(new UserProfileReadModelConfiguration());
        modelBuilder.ApplyConfiguration(new SubscriptionPlanConfiguration());
        modelBuilder.ApplyConfiguration(new UserVoiceModelConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
