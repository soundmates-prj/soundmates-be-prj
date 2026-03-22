using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Application.Interfaces;
using AccountContentService.Application.Services;
using AccountContentService.Domain.Interfaces;
using AccountContentService.Infrastructure.Configurations;
using AccountContentService.Infrastructure.Integrations.Services;
using AccountContentService.Infrastructure.Messaging;
using AccountContentService.Infrastructure.NotificationService.PaymentGateway;
using AccountContentService.Infrastructure.Persistence;
using AccountContentService.Infrastructure.Repositories;
using AccountContentService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AccountContentService.Infrastructure.Extensions
{
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
            services.AddScoped<ISystemSettingReposiotry, SystemSettingReposiotry>();
            services.AddScoped<IServiceConfigRepository, ServiceConfigRepository>();
            services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();

            // VNPay Config
            services.Configure<VNPayConfig>(configuration.GetSection("VNPay"));

            // Payment Providers
            services.AddScoped<IPaymentProvider, VNPayService>();
            services.AddScoped<IPaymentProvider, PayOSService>();

            // Gateway
            services.AddScoped<IPaymentGateway, PaymentGatewayFactory>();

            //Services
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ISystemConfigService, SystemConfigService>();
            services.AddSingleton<IEncryptionService, EncryptionService>();
            services.AddScoped<IUserServiceClient, UserServiceClient>();
            services.AddSingleton<IGeminiConfigEventPublisher, GeminiConfigEventPublisher>();

            services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));


            return services;
        }
    }
}