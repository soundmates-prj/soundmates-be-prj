using DotNetEnv;

namespace AuthService.Api.Bootstrap;

/// <summary>
/// Extension methods for environment configuration
/// Handles .env loading and environment variable mapping
/// </summary>
public static class ConfigurationExtensions
{
    public static WebApplicationBuilder AddEnvironmentConfig(this WebApplicationBuilder builder)
    {
        // Load .env file from solution root (parent directory)
        try
        {
            var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
            if (File.Exists(envPath))
            {
                Env.Load(envPath);
            }
            else
            {
                // Try current directory
                Env.Load();
            }
        }
        catch (Exception)
        {
            // .env file not found, will use appsettings.json or environment variables
        }

        // Map environment variables to configuration
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

            // Map PostgreSQL environment variables
            if (key.StartsWith("POSTGRES_"))
            {
                var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
                var port = Environment.GetEnvironmentVariable("POSTGRES_PORT");
                var database = Environment.GetEnvironmentVariable("POSTGRES_DATABASE");
                var username = Environment.GetEnvironmentVariable("POSTGRES_USERNAME");
                var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
                
                if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(port))
                {
                    var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Disable;Trust Server Certificate=True;";
                    configuration["ConnectionStrings:DefaultConnection"] = connectionString;
                }
            }
            
            // Map RabbitMQ environment variables
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
            
            // Map JWT environment variables
            else if (key == "JWT_KEY")
                configuration["Jwt:Key"] = value;
            else if (key == "JWT_ISSUER")
                configuration["Jwt:Issuer"] = value;
            else if (key == "JWT_AUDIENCE")
                configuration["Jwt:Audience"] = value;
            else if (key == "JWT_EXPIRES_IN_HOURS")
                configuration["Jwt:ExpiresInHours"] = value;
            
            // Map Google OAuth environment variables
            else if (key == "GOOGLE_CLIENT_ID")
                configuration["GoogleOAuth:ClientId"] = value;
            else if (key == "GOOGLE_CLIENT_SECRET")
                configuration["GoogleOAuth:ClientSecret"] = value;
            
            // Map Email environment variables
            else if (key == "EMAIL_HOST")
                configuration["EmailSettings:Host"] = value;
            else if (key == "EMAIL_PORT")
                configuration["EmailSettings:Port"] = value;
            else if (key == "EMAIL_FROM")
                configuration["EmailSettings:From"] = value;
            else if (key == "EMAIL_USERNAME")
                configuration["EmailSettings:Username"] = value;
            else if (key == "EMAIL_PASSWORD")
                configuration["EmailSettings:Password"] = value;
            
            // Map App Settings environment variables
            else if (key == "FRONTEND_URL")
                configuration["AppSettings:FrontendUrl"] = value;
        }
    }
}
