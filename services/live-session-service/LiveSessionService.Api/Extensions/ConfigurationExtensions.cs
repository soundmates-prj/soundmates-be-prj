using DotNetEnv;

namespace LiveSessionService.Api.Extensions;

/// <summary>
/// Extension methods for environment configuration.
/// Handles .env loading and environment variable mapping.
/// </summary>
public static class ConfigurationExtensions
{
    public static WebApplicationBuilder AddEnvironmentConfig(this WebApplicationBuilder builder)
    {
        TryLoadRootEnv();
        builder.Configuration.AddEnvironmentVariables();
        ApplyLegacyAliases(builder.Configuration);

        return builder;
    }

    private static void TryLoadRootEnv()
    {
        try
        {
            var candidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../..", ".env")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../.env")),
                Path.Combine(AppContext.BaseDirectory, ".env")
            };

            var envFile = candidates.FirstOrDefault(File.Exists);
            if (envFile is not null)
            {
                Env.Load(envFile);
            }
        }
        catch
        {
            // Use injected environment variables when .env is unavailable.
        }
    }

    private static void ApplyLegacyAliases(IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            configuration["ConnectionStrings:DefaultConnection"] = connectionString;
        }
        else
        {
            var host = Environment.GetEnvironmentVariable("DB_HOST") ?? Environment.GetEnvironmentVariable("POSTGRES_HOST");
            var port = Environment.GetEnvironmentVariable("DB_PORT") ?? Environment.GetEnvironmentVariable("POSTGRES_PORT");
            var database = Environment.GetEnvironmentVariable("DB_NAME") ?? Environment.GetEnvironmentVariable("POSTGRES_DATABASE");
            var username = Environment.GetEnvironmentVariable("DB_USER") ?? Environment.GetEnvironmentVariable("POSTGRES_USERNAME");
            var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

            if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(port) &&
                !string.IsNullOrWhiteSpace(database) && !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                configuration["ConnectionStrings:DefaultConnection"] =
                    $"Host={host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Disable;Trust Server Certificate=True;";
            }
        }

        configuration["RabbitMq:HostName"] = Environment.GetEnvironmentVariable("RabbitMq__HostName")
            ?? Environment.GetEnvironmentVariable("RabbitMQ__Host")
            ?? Environment.GetEnvironmentVariable("RABBITMQ_HOST")
            ?? configuration["RabbitMq:HostName"];

        configuration["RabbitMq:Port"] = Environment.GetEnvironmentVariable("RabbitMq__Port")
            ?? Environment.GetEnvironmentVariable("RabbitMQ__Port")
            ?? Environment.GetEnvironmentVariable("RABBITMQ_PORT")
            ?? configuration["RabbitMq:Port"];

        configuration["RabbitMq:UserName"] = Environment.GetEnvironmentVariable("RabbitMq__UserName")
            ?? Environment.GetEnvironmentVariable("RabbitMQ__Username")
            ?? Environment.GetEnvironmentVariable("RABBITMQ_USERNAME")
            ?? configuration["RabbitMq:UserName"];

        configuration["RabbitMq:Password"] = Environment.GetEnvironmentVariable("RabbitMq__Password")
            ?? Environment.GetEnvironmentVariable("RabbitMQ__Password")
            ?? Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
            ?? configuration["RabbitMq:Password"];

        configuration["RabbitMq:VirtualHost"] = Environment.GetEnvironmentVariable("RabbitMq__VirtualHost")
            ?? Environment.GetEnvironmentVariable("RabbitMQ__VirtualHost")
            ?? Environment.GetEnvironmentVariable("RABBITMQ_VIRTUALHOST")
            ?? configuration["RabbitMq:VirtualHost"];

        var secret = Environment.GetEnvironmentVariable("Jwt__Secret")
            ?? Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? Environment.GetEnvironmentVariable("JWT_KEY");
        if (!string.IsNullOrWhiteSpace(secret))
        {
            configuration["Jwt:Secret"] = secret;
            configuration["Jwt:Key"] = secret;
        }

        configuration["Jwt:Issuer"] = Environment.GetEnvironmentVariable("Jwt__Issuer")
            ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
            ?? configuration["Jwt:Issuer"];

        configuration["Jwt:Audience"] = Environment.GetEnvironmentVariable("Jwt__Audience")
            ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
            ?? configuration["Jwt:Audience"];

        configuration["AzuraCast:BaseUrl"] = Environment.GetEnvironmentVariable("AzuraCast__BaseUrl")
            ?? Environment.GetEnvironmentVariable("AZURACAST_BASE_URL")
            ?? configuration["AzuraCast:BaseUrl"];

        configuration["AzuraCast:ApiKey"] = Environment.GetEnvironmentVariable("AzuraCast__ApiKey")
            ?? Environment.GetEnvironmentVariable("AZURACAST_API_KEY")
            ?? configuration["AzuraCast:ApiKey"];
    }
}
