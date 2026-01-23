using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Configuration;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Jwt;
using AuthService.Infrastructure.Messaging;
using AuthService.Infrastructure.Repositories;
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

        // Outbox + background publisher
        services.AddScoped<IOutbox, EfCoreOutbox>();
        services.AddHostedService<OutboxPublisherBackgroundService>();

        // RabbitMQ message bus publisher
        services.AddSingleton<IMessageBusPublisher, RabbitMqPublisher>();

        return services;
    }
}
