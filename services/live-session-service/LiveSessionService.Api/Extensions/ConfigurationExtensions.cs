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
        try
        {
            var envCandidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "../../..", ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"),
                Path.Combine(AppContext.BaseDirectory, ".env"),
            };

            var envFile = envCandidates.FirstOrDefault(File.Exists);
            if (envFile is not null)
            {
                Env.Load(envFile);
            }
        }
        catch
        {
            // .env file not found, use appsettings.json or real environment variables
        }

        MapEnvironmentVariables(builder.Configuration);

        return builder;
    }

    private static void MapEnvironmentVariables(IConfiguration configuration)
    {
        foreach (System.Collections.DictionaryEntry envVar in Environment.GetEnvironmentVariables())
        {
            var key = envVar.Key?.ToString();
            var value = envVar.Value?.ToString();

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                continue;

            if (key.StartsWith("POSTGRES_"))
            {
                var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
                var port = Environment.GetEnvironmentVariable("POSTGRES_PORT");
                var database = Environment.GetEnvironmentVariable("POSTGRES_DATABASE");
                var username = Environment.GetEnvironmentVariable("POSTGRES_USERNAME");
                var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

                if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(port))
                {
                    var connectionString =
                        $"Host={host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Disable;Trust Server Certificate=True;";
                    configuration["ConnectionStrings:DefaultConnection"] = connectionString;
                }
            }
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
            else if (key == "JWT_KEY")
                configuration["Jwt:Key"] = value;
            else if (key == "JWT_ISSUER")
                configuration["Jwt:Issuer"] = value;
            else if (key == "JWT_AUDIENCE")
                configuration["Jwt:Audience"] = value;
            else if (key == "AZURACAST_BASE_URL")
                configuration["AzuraCast:BaseUrl"] = value;
            else if (key == "AZURACAST_API_KEY")
                configuration["AzuraCast:ApiKey"] = value;
        }
    }
}
