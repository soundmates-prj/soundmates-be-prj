using AccountContentService.Application.Interfaces;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Application.Services;
using AccountContentService.Infrastructure.Configurations;
using AccountContentService.Infrastructure.Integrations.Services;
using AccountContentService.Infrastructure.Messaging;
using AccountContentService.Infrastructure.Messaging.Consumers;
using AccountContentService.Infrastructure.Messaging.Consumers.Notifications;
using AccountContentService.Infrastructure.NotificationService.PaymentGateway;
using AccountContentService.Infrastructure.Persistence;
using AccountContentService.Infrastructure.Repositories;
using AccountContentService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AccountContentService.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Repositories
        services.AddScoped<IBlogPostRepository, BlogPostRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IPostReactionRepository, ReactionRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ISystemSettingReposiotry, SystemSettingReposiotry>();
        services.AddScoped<IThemeRepository, ThemeRepository>();

        // EDA — User Profile Read Model (local projection via RabbitMQ events)
        services.AddScoped<IUserProfileReadModelRepository, UserProfileReadModelRepository>();
        services.AddScoped<IUserProfileCache, UserProfileCache>();

        // EDA — RabbitMQ user-event consumer
        services.AddScoped<UserEventConsumer>();
        services.AddHostedService<UserEventConsumerHostedService>();
        services.AddScoped<NotificationEventConsumer>();
        services.AddHostedService<NotificationEventConsumerHostedService>();

        // Payment configs
        services.Configure<VNPayConfig>(configuration.GetSection("VNPay"));
        services.Configure<PayOSConfig>(configuration.GetSection("PayOS"));

        // Payment providers
        services.AddScoped<IPaymentProvider, VNPayService>();
        services.AddScoped<IPaymentProvider, PayOSService>();
        services.AddScoped<IPaymentGateway, PaymentGatewayFactory>();

        // Application services
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddSingleton<IEncryptionService, EncryptionService>();

        // AzuraCast validator (uses named HttpClient)
        services.AddHttpClient<IAzuraCastValidator, AzuraCastValidator>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // Config event publishers (RabbitMQ)
        services.AddSingleton<IGeminiConfigEventPublisher, GeminiConfigEventPublisher>();
        services.AddSingleton<IAzuraCastConfigEventPublisher, AzuraCastConfigEventPublisher>();
        services.AddScoped<IMessageBusPublisher, RabbitMqPublisher>();

        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));

        return services;
    }
}
