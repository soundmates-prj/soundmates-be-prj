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

}