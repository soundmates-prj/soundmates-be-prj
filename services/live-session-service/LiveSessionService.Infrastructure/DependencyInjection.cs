using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Features.Common.AzuraCast;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using LiveSessionService.Infrastructure.Services;
using LiveSessionService.Infrastructure.Repositories;
using LiveSessionService.Infrastructure.Services.AzuraCast;
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
        // PRIORITY: Environment variables FIRST (Docker), then config (local)
        var postgresHost = Environment.GetEnvironmentVariable("POSTGRES_HOST");
        
        string connectionString;
        
        if (!string.IsNullOrEmpty(postgresHost))
        {
            // Build from environment variables (Docker/Production)
            var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
            var database = Environment.GetEnvironmentVariable("POSTGRES_DATABASE") ?? "live_session_db";
            var username = Environment.GetEnvironmentVariable("POSTGRES_USERNAME") ?? "postgres";
            var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
            
            connectionString = $"Host={postgresHost};Port={port};Database={database};Username={username};Password={password}";
            
            Console.WriteLine($"[DEBUG] Built connection string from ENVIRONMENT VARIABLES:");
            Console.WriteLine($"  Host={postgresHost}, Port={port}, Database={database}, Username={username}");
        }
        else
        {
            // Fallback to appsettings.json (Local development)
            connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found and no POSTGRES_HOST env var");
            
            Console.WriteLine($"[DEBUG] Using connection string from appsettings.json (POSTGRES_HOST not set)");
        }

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
        services.AddScoped<IStationPlaylistRepository, StationPlaylistRepository>();
        services.AddScoped<IMediaFileRepository, MediaFileRepository>();
        services.AddScoped<IPlaylistMediaRepository, PlaylistMediaRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        // External Services - AzuraCast
        // Read from environment variables (Docker) or config
        var azuraCastBaseUrl = Environment.GetEnvironmentVariable("AZURACAST_BASE_URL")
            ?? configuration["AzuraCast:BaseUrl"]
            ?? throw new InvalidOperationException("AZURACAST_BASE_URL not configured");
        
        var azuraCastApiKey = Environment.GetEnvironmentVariable("AZURACAST_API_KEY")
            ?? configuration["AzuraCast:ApiKey"];

        // Debug: verify API key is loaded (mask the secret part)
        if (string.IsNullOrWhiteSpace(azuraCastApiKey))
        {
            Console.WriteLine("[WARNING] AzuraCast API key is NOT configured. " +
                "Set AzuraCast__ApiKey in .env or AZURACAST_API_KEY as an environment variable. " +
                "All authenticated AzuraCast endpoints will return 403 NotLoggedInException.");
        }
        else
        {
            var masked = azuraCastApiKey.Length > 8
                ? azuraCastApiKey[..4] + "****" + azuraCastApiKey[^4..]
                : "****";
            Console.WriteLine($"[DEBUG] AzuraCast API key loaded: {masked} (length={azuraCastApiKey.Length})");
        }

        Console.WriteLine($"[DEBUG] AzuraCast BaseUrl: {azuraCastBaseUrl}");

        services.AddHttpClient<IAzuraCastClient, AzuraCastClient>(client =>
        {
            client.BaseAddress = new Uri(azuraCastBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(120);
            if (!string.IsNullOrWhiteSpace(azuraCastApiKey))
                client.DefaultRequestHeaders.Add("X-API-Key", azuraCastApiKey);
        });

        // Messaging - RabbitMQ
        services.AddSingleton<IMessageBusPublisher, RabbitMqPublisher>();
        services.AddHostedService<OutboxPublisherBackgroundService>();

        return services;
    }
}
