using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Features.Common.AzuraCast;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using LiveSessionService.Infrastructure.Services;
using LiveSessionService.Infrastructure.Repositories;
using LiveSessionService.Infrastructure.ExternalServices.AzuraCast;
using LiveSessionService.Infrastructure.Messaging;
using LiveSessionService.Infrastructure.Messaging.Outbox;
using AuthService.Infrastructure.Messaging;

namespace LiveSessionService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database - PostgreSQL
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

        services.AddDbContext<LiveSessionDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(LiveSessionDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            }));

        // DateTime Provider
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Repositories
        services.AddScoped<ILiveSessionRepository, LiveSessionRepository>();
        services.AddScoped<INowPlayingHistoryRepository, NowPlayingHistoryRepository>();
        services.AddScoped<IAzuraCastStationRepository, AzuraCastStationRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        // External Services - AzuraCast
        // BaseUrl config qua HttpClient DI (Clean Architecture compliant)
        var azuraCastBaseUrl = configuration["AzuraCast:BaseUrl"] 
            ?? throw new InvalidOperationException("AzuraCast:BaseUrl not configured");
        
        services.AddHttpClient<IAzuraCastClient, AzuraCastClient>(client =>
        {
            client.BaseAddress = new Uri(azuraCastBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // Messaging - RabbitMQ
        services.AddSingleton<IMessageBusPublisher, RabbitMqPublisher>();
        services.AddHostedService<OutboxPublisherBackgroundService>();

        return services;
    }
}
