using DotNetEnv;

namespace AuthQueryService.Api.Extensions;

/// <summary>
/// Extension methods for environment configuration.
/// Loads .env file and maps environment variables into IConfiguration,
/// so secrets never need to be hardcoded in appsettings files.
/// </summary>
public static class ConfigurationExtensions
{
    public static WebApplicationBuilder AddEnvironmentConfig(this WebApplicationBuilder builder)
    {
        // Load .env — try multiple candidate paths so it works from VS, dotnet run, and Docker.
        // In production/Docker, real env vars are already set — .env won't exist and that's fine.
        try
        {
            var candidates = new[]
            {
                // bin/Debug/<tfm> → up three levels to the project folder
                Path.Combine(AppContext.BaseDirectory, "../../..", ".env"),
                // dotnet run from project dir
                Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                // one level up (older layout)
                Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"),
                // published output dir
                Path.Combine(AppContext.BaseDirectory, ".env"),
            };

            var envFile = candidates.FirstOrDefault(File.Exists);
            if (envFile is not null)
                Env.Load(envFile);
        }
        catch
        {
            // Silently continue — real env vars (Docker/CI) take precedence anyway.
        }

        MapEnvironmentVariables(builder.Configuration);
        return builder;
    }

    private static void MapEnvironmentVariables(IConfiguration configuration)
    {
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var key   = entry.Key?.ToString();
            var value = entry.Value?.ToString();

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                continue;

            // ── PostgreSQL ────────────────────────────────────────────────
            if (key.StartsWith("POSTGRES_"))
            {
                var host     = Environment.GetEnvironmentVariable("POSTGRES_HOST");
                var port     = Environment.GetEnvironmentVariable("POSTGRES_PORT");
                var database = Environment.GetEnvironmentVariable("POSTGRES_DATABASE");
                var username = Environment.GetEnvironmentVariable("POSTGRES_USERNAME");
                var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

                if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(port))
                    configuration["ConnectionStrings:DefaultConnection"] =
                        $"Host={host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Disable;Trust Server Certificate=True;";
            }

            // ── MongoDB ───────────────────────────────────────────────────
            else if (key == "MONGODB_CONNECTION_STRING")
                configuration["ConnectionStrings:MongoDb"] = value;
            else if (key == "MONGODB_DATABASE")
                configuration["Mongo:Database"] = value;

            // ── RabbitMQ ──────────────────────────────────────────────────
            else if (key == "RABBITMQ_HOST")
                configuration["RabbitMq:HostName"] = value;
            else if (key == "RABBITMQ_PORT")
                configuration["RabbitMq:Port"] = value;
            else if (key == "RABBITMQ_USERNAME")
                configuration["RabbitMq:UserName"] = value;
            else if (key == "RABBITMQ_PASSWORD")
                configuration["RabbitMq:Password"] = value;
            else if (key == "RABBITMQ_VIRTUALHOST")
                configuration["RabbitMq:VirtualHost"] = value;

            // ── JWT ───────────────────────────────────────────────────────
            else if (key == "JWT_KEY")
                configuration["Jwt:Key"] = value;
            else if (key == "JWT_ISSUER")
                configuration["Jwt:Issuer"] = value;
            else if (key == "JWT_AUDIENCE")
                configuration["Jwt:Audience"] = value;
            else if (key == "JWT_EXPIRES_IN_HOURS")
                configuration["Jwt:ExpiresInHours"] = value;

            // ── Spotify ───────────────────────────────────────────────────
            else if (key == "SPOTIFY_CLIENT_ID")
                configuration["Spotify:ClientId"] = value;
            else if (key == "SPOTIFY_CLIENT_SECRET")
                configuration["Spotify:ClientSecret"] = value;
            else if (key == "SPOTIFY_API_BASE")
                configuration["Spotify:ApiBase"] = value;
        }
    }
}
