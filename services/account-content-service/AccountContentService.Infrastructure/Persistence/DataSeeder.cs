using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AccountContentService.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(AccountContentDbContext context)
    {
        var now = DateTime.UtcNow;
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        if (!await context.Themes.AnyAsync(t => t.Name == "Chill Lofi"))
        {
            context.Themes.Add(new Theme
            {
                Id = Guid.NewGuid(),
                Name = "Chill Lofi",
                Mode = "dark",
                IsActive = true,
                PrimaryColor = "#8b5cf6", // Purple
                SecondaryColor = "#c084fc",
                BackgroundColor = "#1e1b4b", // Deep dark purple
                TextColor = "#f1f5f9",
                Mood = "chill",
                FontFamily = "'Inter', sans-serif",
                GradientBackground = "linear-gradient(to bottom, #1e1b4b, #312e81)",
                PlayerColor = "#a855f7",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ConfigJson = System.Text.Json.JsonDocument.Parse("""
                {
                    "borderRadius": "12px",
                    "boxShadow": "0 4px 20px rgba(139, 92, 246, 0.2)",
                    "backgroundImage": "https://www.transparenttextures.com/patterns/stardust.png"
                }
                """).RootElement
            });
        }

        if (!await context.Themes.AnyAsync(t => t.Name == "Love Theme"))
        {
            context.Themes.Add(new Theme
            {
                Id = Guid.NewGuid(),
                Name = "Love Theme",
                Mode = "light",
                IsActive = true,
                PrimaryColor = "#FF4D6D", // Soft Red/Pink
                SecondaryColor = "#FF758F",
                BackgroundColor = "#FFF0F3", // Very light pink background
                TextColor = "#590D22", // Deep burgundy for text
                Mood = "love",
                FontFamily = "'Ephesis', 'Inter', cursive",
                GradientBackground = "linear-gradient(135deg, #FFF0F3 0%, #FFCCD5 100%)",
                PlayerColor = "#C9184A",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ConfigJson = System.Text.Json.JsonDocument.Parse("""
                {
                    "borderRadius": "24px",
                    "boxShadow": "0 8px 24px rgba(255, 77, 109, 0.2)",
                    "iconStyle": "heart",
                    "backgroundImage": "https://static.vecteezy.com/system/resources/previews/002/092/177/non_2x/love-heart-pattern-with-dots-and-stars-free-vector.jpg"
                }
                """).RootElement
            });
        }
        else 
        {
            // Update existing Love Theme
            var existing = await context.Themes.FirstAsync(t => t.Name == "Love Theme");
            existing.PrimaryColor = "#FF4D6D";
            existing.SecondaryColor = "#FF758F";
            existing.BackgroundColor = "#FFF0F3";
            existing.TextColor = "#590D22";
            existing.ConfigJson = System.Text.Json.JsonDocument.Parse("""
                {
                    "borderRadius": "24px",
                    "boxShadow": "0 8px 24px rgba(255, 77, 109, 0.2)",
                    "iconStyle": "heart",
                    "backgroundImage": "https://images.unsplash.com/photo-1518199266791-5375a83190b7?auto=format&fit=crop&w=1920&q=80"
                }
                """).RootElement;
        }

        if (!await context.Themes.AnyAsync(t => t.Name == "Orange Cat"))
        {
            context.Themes.Add(new Theme
            {
                Id = Guid.NewGuid(),
                Name = "Orange Cat",
                Mode = "light",
                IsActive = true,
                PrimaryColor = "#FF8C00", // Orange
                SecondaryColor = "#FFB74D", // Lighter orange
                BackgroundColor = "#FFF3E0", // Very light orange/yellow background
                TextColor = "#4E342E", // Dark brown for text
                Mood = "playful",
                FontFamily = "'Comic Neue', 'Nunito', sans-serif", // Playful font
                GradientBackground = "linear-gradient(135deg, #FFF3E0 0%, #FFE0B2 100%)",
                PlayerColor = "#F57C00",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ConfigJson = System.Text.Json.JsonDocument.Parse("""
                {
                    "borderRadius": "16px",
                    "boxShadow": "0 4px 12px rgba(255, 140, 0, 0.15)",
                    "iconStyle": "rounded",
                    "backgroundImage": "https://www.transparenttextures.com/patterns/little-pluses.png",
                    "backgroundSize": "auto"
                }
                """).RootElement
            });
        }

        if (!await context.Themes.AnyAsync(t => t.Name == "Vintage Vinyl Theme"))
        {
            context.Themes.Add(new Theme
            {
                Id = Guid.NewGuid(),
                Name = "Vintage Vinyl Theme",
                Mode = "dark",
                IsActive = true,
                PrimaryColor = "#FFD13B", 
                SecondaryColor = "#1A1A1A", 
                BackgroundColor = "#488EB5", 
                TextColor = "#F5F5DC", 
                Mood = "chill",
                FontFamily = "'M PLUS Rounded 1c', 'Courier New', sans-serif", 
                GradientBackground = "linear-gradient(135deg, #488EB5 0%, #2A6A8C 100%)",
                PlayerColor = "#FFD13B",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ConfigJson = System.Text.Json.JsonDocument.Parse("""
                {
                    "borderRadius": "20px",
                    "boxShadow": "0 8px 24px rgba(179, 136, 255, 0.15)",
                    "backgroundImage": "https://i.postimg.cc/jSrb6523/af911b8119ee0cc0d44c031be361a802.jpg",
                    "backgroundSize": "cover"
                }
                """).RootElement
            });
        }
        else 
        {
            // Update existing Vintage Vinyl Theme
            var existing = await context.Themes.FirstAsync(t => t.Name == "Vintage Vinyl Theme");
            existing.PrimaryColor = "#FFD13B";
            existing.SecondaryColor = "#1A1A1A";
            existing.BackgroundColor = "#488EB5";
            existing.TextColor = "#F5F5DC";
            existing.FontFamily = "'M PLUS Rounded 1c', 'Courier New', sans-serif";
            existing.GradientBackground = "linear-gradient(135deg, #488EB5 0%, #2A6A8C 100%)";
            existing.PlayerColor = "#FFD13B";
            existing.ConfigJson = System.Text.Json.JsonDocument.Parse("""
                {
                    "borderRadius": "20px",
                    "boxShadow": "0 8px 24px rgba(179, 136, 255, 0.15)",
                    "backgroundImage": "https://i.postimg.cc/jSrb6523/af911b8119ee0cc0d44c031be361a802.jpg",
                    "backgroundSize": "cover"
                }
                """).RootElement;
        }
        
        if (!await context.Themes.AnyAsync(t => t.Name == "Neon Cyberpunk"))
        {
            context.Themes.Add(new Theme
            {
                Id = Guid.NewGuid(),
                Name = "Neon Cyberpunk",
                Mode = "dark",
                IsActive = true,
                PrimaryColor = "#00E5FF", // Cyan
                SecondaryColor = "#FF00FF", // Magenta
                BackgroundColor = "#0B0C10", // Very dark futuristic grey
                TextColor = "#E0E6ED",
                Mood = "energetic",
                FontFamily = "'Orbitron', 'Roboto', sans-serif",
                GradientBackground = "linear-gradient(135deg, #0B0C10 0%, #1F2833 100%)",
                PlayerColor = "#00E5FF",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ConfigJson = System.Text.Json.JsonDocument.Parse("""
                {
                    "borderRadius": "8px",
                    "boxShadow": "0 0 15px rgba(0, 229, 255, 0.4)",
                    "iconStyle": "sharp",
                    "backgroundImage": "https://images.unsplash.com/photo-1555680202-c86f0e12f086?auto=format&fit=crop&w=1920&q=80",
                    "backgroundSize": "cover",
                    "backgroundBlendMode": "overlay"
                }
                """).RootElement
            });
        }

        if (!await context.Themes.AnyAsync(t => t.Name == "Sunset Balcony Theme"))
        {
            context.Themes.Add(new Theme
            {
                Id = Guid.NewGuid(),
                Name = "Sunset Balcony Theme",
                Mode = "dark",
                IsActive = true,
                PrimaryColor = "#FF8A65",
                SecondaryColor = "#FFB74D",
                BackgroundColor = "#2A233C",
                TextColor = "#FFE0B2",
                Mood = "sunset",
                FontFamily = "'Poppins', 'Nunito', sans-serif",
                GradientBackground = "linear-gradient(135deg, #2A233C 0%, #4A3B52 60%, #FF8A65 100%)",
                PlayerColor = "#FF5252",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ConfigJson = System.Text.Json.JsonDocument.Parse("""
                {
                    "borderRadius": "12px",
                    "boxShadow": "0 8px 32px rgba(255, 138, 101, 0.25)",
                    "backgroundImage": "https://i.postimg.cc/ZR0F56kY/bcf4f37fcab5a44ddf8c2b4cb6279e84.jpg"
                }
                """).RootElement
            });
        }
        else
        {
            var existing = await context.Themes.FirstAsync(t => t.Name == "Sunset Balcony Theme");
            existing.PrimaryColor = "#FF8A65";
            existing.SecondaryColor = "#FFB74D";
            existing.BackgroundColor = "#2A233C";
            existing.TextColor = "#FFE0B2";
            existing.GradientBackground = "linear-gradient(135deg, #2A233C 0%, #4A3B52 60%, #FF8A65 100%)";
            existing.PlayerColor = "#FF5252";
            existing.ConfigJson = System.Text.Json.JsonDocument.Parse("""
                {
                    "borderRadius": "12px",
                    "boxShadow": "0 8px 32px rgba(255, 138, 101, 0.25)",
                    "backgroundImage": "https://i.postimg.cc/ZR0F56kY/bcf4f37fcab5a44ddf8c2b4cb6279e84.jpg"
                }
                """).RootElement;
        }


        await context.SaveChangesAsync();

        // ── ALWAYS patch existing plan limits in case they were created before the VoiceClone migration ──
        // This handles the case where VoiceModelLimit/TtsMinuteLimit/PodcastRequestLimit defaulted to 0
        // because the columns were added after the plan rows were already inserted.
        var premiumDb = await context.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanName == "Premium");
        if (premiumDb != null && premiumDb.VoiceModelLimit == 0 && premiumDb.TtsMinuteLimit == 0)
        {
            premiumDb.VoiceModelLimit = 1;
            premiumDb.TtsMinuteLimit = 30;
            premiumDb.PodcastRequestLimit = 2;
        }

        var eliteDb = await context.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanName == "Elite");
        if (eliteDb != null && eliteDb.VoiceModelLimit == 0 && eliteDb.TtsMinuteLimit == 0)
        {
            eliteDb.VoiceModelLimit = 3;
            eliteDb.TtsMinuteLimit = 120;
            eliteDb.PodcastRequestLimit = 5;
        }

        await context.SaveChangesAsync();

        if (await context.BlogPosts.AnyAsync()) return; // Data already seeded

        // SUBSCRIPTION PLANS
        var freePlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Free",
            Price = 0,
            DurationDays = 30,
            RequestLimit = 5,
            VoiceModelLimit = 0,
            TtsMinuteLimit = 0,
            PodcastRequestLimit = 0,
            Description = "Trải nghiệm nghe nhạc cơ bản, không cần thanh toán.",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var premiumPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Premium",
            Price = 59000, // VND
            DurationDays = 30,
            RequestLimit = 20,
            VoiceModelLimit = 1,
            TtsMinuteLimit = 30,
            PodcastRequestLimit = 2,
            Description = "Nâng cấp giới hạn và mở khóa AI giọng đọc., giới hạn 30 phút TTS/tháng.",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var elitePlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = "Elite",
            Price = 159000, // VND
            DurationDays = 30,
            RequestLimit = 15,
            VoiceModelLimit = 3,
            TtsMinuteLimit = 120,
            PodcastRequestLimit = 5,
            Description = "Trải nghiệm không giới hạn, tạo nhiều giọng nói AI, 120 phút TTS/tháng, ưu tiên cao nhất.",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.SubscriptionPlans.AddRange(freePlan, premiumPlan, elitePlan);

        // SUBSCRIPTION
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = premiumPlan.Id,
            StartDate = now,
            EndDate = now.AddDays(30),
            Status = "active"
        };

        context.Subscriptions.Add(subscription);

        // BLOG POST
        var post = new BlogPost
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Welcome to Soundmates",
            ContentText = "Welcome to Soundmates! This is the first post.",
            MoodTag = "happy",
            PrivacyScope = "public",
            Status = "published",
            IsGenerated = false,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            PublishedAt = now
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
            CreatedAt = now,
            UpdatedAt = now
        };

        context.BlogComments.Add(comment);

        // REACTION
        var reaction = new PostReaction
        {
            Id = Guid.NewGuid(),
            PostId = post.Id,
            UserId = userId,
            ReactionType = "like",
            CreatedAt = now
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
            CreatedAt = now
        };

        context.ContentReports.Add(report);

        // PAYMENT
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TargetType = PaymentTargetType.Subscription.ToString(),
            TargetId = premiumPlan.Id,
            TotalAmount = 9.99m,
            Status = "completed",
            CreatedAt = now,
            UpdatedAt = now,
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
            PaymentAt = now,
            TransactionStatus = "success",
            CreatedAt = now,
            UpdatedAt = now
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
            CreatedAt = now,
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
            CreatedAt = now
        };

        context.Notifications.Add(notification);

        await context.SaveChangesAsync();
    }
}