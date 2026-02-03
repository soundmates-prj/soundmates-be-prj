using AuthService.Application.Configuration;
using AuthService.Application.Features.Common;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Messaging.MessageBus;
using AuthService.Infrastructure.Messaging.Outbox;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Repositories;
using AuthService.Infrastructure.Security.Jwt;
using AuthService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // EF Core DbContext (PostgreSQL)
        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories (direct DbContext access - DAO layer removed)
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        // JWT token generator
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // Google OAuth service
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        // Email service
        services.Configure<EmailOptions>(configuration.GetSection("EmailSettings"));
        services.AddScoped<IEmailService, EmailService>();

        // App settings (Frontend URL, etc.)
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

        // DateTime provider (infrastructure concern)
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Application services (implemented in infrastructure)
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IOtpService, OtpService>();

        // Message Bus Publisher (RabbitMQ)
        services.AddSingleton<IMessageBusPublisher, RabbitMqPublisher>();

        // Outbox Background Publisher Service
        services.AddHostedService<OutboxPublisherBackgroundService>();

        return services;
    }
}
