using AccountContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccountContentService.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(AccountContentDbContext context)
    {
        if (await context.BlogPosts.AnyAsync())
            return;

        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // THEMES
        var defaultTheme = new Theme
        {
            Id = Guid.NewGuid(),
            ThemeName = "Default",
            PrimaryColor = "#4F46E5",
            SecondaryColor = "#FFFFFF",
            FontFamily = "Inter",
            IsActive = true
        };

        var darkTheme = new Theme
        {
            Id = Guid.NewGuid(),
            ThemeName = "Dark",
            PrimaryColor = "#111827",
            SecondaryColor = "#1F2937",
            FontFamily = "Inter",
            IsActive = true
        };

        context.Themes.AddRange(defaultTheme, darkTheme);

        // SUBSCRIPTION PLANS
        var freePlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Free",
            Price = 0,
            DurationDays = 30,
            RequestLimit = 5,
            Description = "Free tier",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var premiumPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Premium",
            Price = 9.99m,
            DurationDays = 30,
            RequestLimit = 100,
            Description = "Premium plan",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.SubscriptionPlans.AddRange(freePlan, premiumPlan);

        // SUBSCRIPTION
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = premiumPlan.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = "active"
        };

        context.Subscriptions.Add(subscription);

        // BLOG POST
        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ContentText = "Welcome to Soundmates!",
            MoodTag = "happy",
            PrivacyScope = "public",
            IsGenerated = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            PublishedAt = DateTime.UtcNow
        };

        context.BlogPosts.Add(post);

        // COMMENT
        var comment = new BlogComment
        {
            Id = Guid.NewGuid(),
            PostId = post.Id,
            UserId = userId,
            Content = "Great post!",
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.BlogComments.Add(comment);

        // REACTION
        var reaction = new PostReaction
        {
            Id = Guid.NewGuid(),
            PostId = post.Id,
            UserId = userId,
            ReactionType = "like",
            CreatedAt = DateTime.UtcNow
        };

        context.PostReactions.Add(reaction);

        // CONTENT REPORT
        var report = new ContentReport
        {
            Id = Guid.NewGuid(),
            ContentType = "post",
            ContentId = post.Id,
            Reason = "Spam",
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        context.ContentReports.Add(report);

        // PAYMENT
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = premiumPlan.Id,
            TargetType = "subscription",
            TargetId = subscription.Id,
            TotalAmount = 9.99m,
            Status = "completed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExternalReference = "PAY123456"
        };

        context.Payments.Add(payment);

        // PAYMENT TRANSACTION
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            PaymentProvider = "Stripe",
            PaymentMethod = "card",
            Amount = 9.99m,
            PaymentAt = DateTime.UtcNow,
            TransactionStatus = "success",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.PaymentTransactions.Add(transaction);

        // WEBHOOK LOG
        var webhook = new PaymentWebhookLog
        {
            Id = Guid.NewGuid(),
            Provider = "Stripe",
            Payload = "{status:'success'}",
            Signature = "abc123",
            Processed = true,
            CreatedAt = DateTime.UtcNow,
            TransactionId = transaction.Id
        };

        context.PaymentWebhookLogs.Add(webhook);

        // NOTIFICATION
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = "PAYMENT_SUCCESS",
            ReferenceId = payment.Id,
            Message = "Your payment was successful",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Notifications.Add(notification);

        await context.SaveChangesAsync();
    }
}