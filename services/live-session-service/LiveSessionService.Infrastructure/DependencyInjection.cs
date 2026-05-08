using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Features.Common.AzuraCast;
using LiveSessionService.Application.Services;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using LiveSessionService.Infrastructure.Services;
using LiveSessionService.Infrastructure.Repositories;
using LiveSessionService.Infrastructure.Services.AzuraCast;
using LiveSessionService.Infrastructure.Messaging;
using LiveSessionService.Infrastructure.Messaging.Outbox;
using LiveSessionService.Infrastructure.Services.Cloudinary;

namespace LiveSessionService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database - PostgreSQL
        // PRIORITY: DB_HOST env var (docker-compose standard) > POSTGRES_HOST (legacy) > config
        var postgresHost = Environment.GetEnvironmentVariable("DB_HOST")
            ?? Environment.GetEnvironmentVariable("POSTGRES_HOST");

        string connectionString;

        if (!string.IsNullOrEmpty(postgresHost))
        {
            // Build from environment variables (Docker/Production)
            var port = Environment.GetEnvironmentVariable("DB_PORT")
                ?? Environment.GetEnvironmentVariable("POSTGRES_PORT")
                ?? "5432";
            var database = Environment.GetEnvironmentVariable("LIVE_SESSION_DB_NAME")
                ?? Environment.GetEnvironmentVariable("DB_NAME")
                ?? Environment.GetEnvironmentVariable("POSTGRES_DATABASE")
                ?? "live_session_db";
            var username = Environment.GetEnvironmentVariable("DB_USER")
                ?? Environment.GetEnvironmentVariable("POSTGRES_USERNAME")
                ?? "postgres";
            var password = Environment.GetEnvironmentVariable("DB_PASSWORD")
                ?? Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")
                ?? "postgres";

            connectionString =
                $"Host={postgresHost};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Disable;Trust Server Certificate=True;";

            Console.WriteLine($"[DEBUG] Built connection string from ENVIRONMENT VARIABLES:");
            Console.WriteLine($"  Host={postgresHost}, Port={port}, Database={database}, Username={username}");
        }
        else
        {
            // Fallback to appsettings.json (Local development)
            connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found and no DB_HOST env var");

            Console.WriteLine($"[DEBUG] Using connection string from appsettings.json (DB_HOST not set)");
        }

        services.AddDbContext<LiveSessionDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });

            // Suppress PendingModelChangesWarning: occurs when the in-memory EF model has
            // changes not yet scaffolded into a migration (e.g. new entities added).
            // This is safe to ignore during startup migrations — the schema may already
            // be in sync with the DB; the warning just means the snapshot is out of date.
            options.ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        // Register the interface to resolve to the DbContext
        services.AddScoped<LiveSessionService.Application.Abstractions.Persistence.ILiveSessionDbContext>(provider => provider.GetRequiredService<LiveSessionDbContext>());

        // DateTime Provider
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Application Services
        services.AddScoped<InputValidationService>();
        services.AddScoped<SyncConfigurationService>();
        services.AddScoped<AzuraCastErrorHandler>();

        // Repositories
        services.AddScoped<ILiveSessionRepository, LiveSessionRepository>();
        services.AddScoped<ISessionScheduleRepository, SessionScheduleRepository>();
        services.AddScoped<IUserPlaylistRepository, UserPlaylistRepository>();
        services.AddScoped<INowPlayingHistoryRepository, NowPlayingHistoryRepository>();
        services.AddScoped<IAzuraCastStationRepository, AzuraCastStationRepository>();
        services.AddScoped<IStationPlaylistRepository, StationPlaylistRepository>();
        services.AddScoped<IMediaFileRepository, MediaFileRepository>();
        services.AddScoped<IStationMediaFileRepository, StationMediaFileRepository>();
        services.AddScoped<IPlaylistMediaRepository, PlaylistMediaRepository>();
        services.AddScoped<ISongRequestRepository, SongRequestRepository>();
        services.AddScoped<IPodcastRepository, PodcastRepository>();
        services.AddScoped<IUserSavedPodcastRepository, UserSavedPodcastRepository>();
        services.AddScoped<IPodcastRequestRepository, PodcastRequestRepository>();
        services.AddScoped<IPodcastEpisodeRequestRepository, PodcastEpisodeRequestRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        // Cloudinary media storage
        services.AddHttpClient("CloudinaryDownload");
        services.AddSingleton<ICloudinaryMediaStorage, CloudinaryMediaStorage>();

        // Audio Moderation
        services.AddScoped<IAudioModerationService, AudioModerationService>();

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
        services.AddHttpClient<AzuraCastPodcastService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(300); // Long timeout for download + conversion
        });
        services.AddScoped<IAzuraCastPodcastService, AzuraCastPodcastService>();

        // Internal Services
        // The URL should point to the internal Docker container name and port used by account-content-service
        var accountContentBaseUrl = Environment.GetEnvironmentVariable("ACCOUNT_CONTENT_URL") ?? "http://account-content-service:8080/";
        services.AddHttpClient<IAccountContentClient, AccountContentServiceClient>(client =>
        {
            client.BaseAddress = new Uri(accountContentBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Messaging - RabbitMQ
        services.AddSingleton<IMessageBusPublisher, RabbitMqPublisher>();
        services.AddHostedService<OutboxPublisherBackgroundService>();

        // RabbitMQ options for AzuraCast config consumer
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.AddHostedService<AzuraCastConfigEventConsumer>();

        return services;
    }
}
