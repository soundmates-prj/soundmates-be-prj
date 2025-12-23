using System;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Dao;
using AuthService.Infrastructure.Dao.Interfaces;
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Jwt;
using AuthService.Infrastructure.Messaging;
using AuthService.Infrastructure.Repositories;
using AuthService.Infrastructure.Services;
using AuthService.Application.Common;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using AuthService.Application.Abstractions.Messaging;

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

        // DAOs (write side - PostgreSQL)
        services.AddScoped<IAuthDao, AuthDao>();
        services.AddScoped<IUserDao, UserDao>();
        services.AddScoped<IRoleDao, RoleDao>();
        services.AddScoped<IProfileDAO, ProfileDAO>();

        // Repositories (write side)
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();

        // JWT token generator
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // Google OAuth service
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        // Email service
        services.Configure<EmailOptions>(configuration.GetSection("EmailSettings"));
        services.AddScoped<IEmailService, EmailService>();

        // App settings (Frontend URL, etc.)
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

        // OTP repository
        services.AddScoped<IOtpRepository, OtpRepository>();

        // Outbox + background publisher
        services.AddScoped<IOutbox, EfCoreOutbox>();
        services.AddHostedService<OutboxPublisherBackgroundService>();

        // Remove IConnection singleton; register publisher directly.
        services.AddSingleton<IMessageBusPublisher, RabbitMqPublisher>();

        return services;
    }
}