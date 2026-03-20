using DotNetEnv;

namespace AccountContentService.Api.Extensions;

public static class ConfigurationExtensions
{
    public static WebApplicationBuilder AddEnvironmentConfig(this WebApplicationBuilder builder)
    {
        LoadEnvFile();

        MapPostgres(builder.Configuration);
        MapRabbitMq(builder.Configuration);
        MapJwt(builder.Configuration);
        MapGoogle(builder.Configuration);
        MapEmail(builder.Configuration);
        MapVNPay(builder.Configuration);
        MapAESEncryption(builder.Configuration);
        MapApp(builder.Configuration);

        return builder;
    }

    private static void LoadEnvFile()
    {
        try
        {
            var envCandidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                Path.Combine(AppContext.BaseDirectory, ".env"),
                Path.Combine(AppContext.BaseDirectory, "../.env"),
                Path.Combine(AppContext.BaseDirectory, "../../..", ".env")
            };

            var envFile = envCandidates.FirstOrDefault(File.Exists);

            if (envFile != null)
            {
                Env.Load(envFile);
            }
        }
        catch
        {
            // Ignore if .env not found
        }
    }

    private static void MapPostgres(IConfiguration configuration)
    {
        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT");
        var database = Environment.GetEnvironmentVariable("POSTGRES_DATABASE");
        var username = Environment.GetEnvironmentVariable("POSTGRES_USERNAME");
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

        if (!string.IsNullOrEmpty(host))
        {
            var connectionString =
                $"Host={host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Disable;Trust Server Certificate=True;";

            configuration["ConnectionStrings:DefaultConnection"] = connectionString;
        }
    }

    private static void MapRabbitMq(IConfiguration configuration)
    {
        configuration["RabbitMq:HostName"] =
            Environment.GetEnvironmentVariable("RABBITMQ_HOST");

        configuration["RabbitMq:Port"] =
            Environment.GetEnvironmentVariable("RABBITMQ_PORT");

        configuration["RabbitMq:UserName"] =
            Environment.GetEnvironmentVariable("RABBITMQ_USERNAME");

        configuration["RabbitMq:Password"] =
            Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD");

        configuration["RabbitMq:VirtualHost"] =
            Environment.GetEnvironmentVariable("RABBITMQ_VIRTUALHOST");
    }

    private static void MapJwt(IConfiguration configuration)
    {
        configuration["Jwt:Key"] =
            Environment.GetEnvironmentVariable("JWT_KEY");

        configuration["Jwt:Issuer"] =
            Environment.GetEnvironmentVariable("JWT_ISSUER");

        configuration["Jwt:Audience"] =
            Environment.GetEnvironmentVariable("JWT_AUDIENCE");

        configuration["Jwt:ExpiresInHours"] =
            Environment.GetEnvironmentVariable("JWT_EXPIRES_IN_HOURS");
    }

    private static void MapGoogle(IConfiguration configuration)
    {
        configuration["GoogleOAuth:ClientId"] =
            Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");

        configuration["GoogleOAuth:ClientSecret"] =
            Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
    }

    private static void MapEmail(IConfiguration configuration)
    {
        configuration["EmailSettings:Host"] =
            Environment.GetEnvironmentVariable("EMAIL_HOST");

        configuration["EmailSettings:Port"] =
            Environment.GetEnvironmentVariable("EMAIL_PORT");

        configuration["EmailSettings:From"] =
            Environment.GetEnvironmentVariable("EMAIL_FROM");

        configuration["EmailSettings:Username"] =
            Environment.GetEnvironmentVariable("EMAIL_USERNAME");

        configuration["EmailSettings:Password"] =
            Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
    }

    private static void MapVNPay(IConfiguration configuration)
    {
        var tmnCode = Environment.GetEnvironmentVariable("VNPAY_TMN_CODE");
        var hashSecret = Environment.GetEnvironmentVariable("VNPAY_HASH_SECRET");
        var baseUrl = Environment.GetEnvironmentVariable("VNPAY_BASE_URL");
        var returnUrl = Environment.GetEnvironmentVariable("VNPAY_RETURN_URL");

        if (!string.IsNullOrWhiteSpace(tmnCode))
            configuration["VNPay:TmnCode"] = tmnCode;

        if (!string.IsNullOrWhiteSpace(hashSecret))
            configuration["VNPay:HashSecret"] = hashSecret;

        if (!string.IsNullOrWhiteSpace(baseUrl))
            configuration["VNPay:BaseUrl"] = baseUrl;

        if (!string.IsNullOrWhiteSpace(returnUrl))
            configuration["VNPay:ReturnUrl"] = returnUrl;
    }

    private static void MapAESEncryption(IConfiguration configuration)
    {
        configuration["AESEncryption:Key"] =
            Environment.GetEnvironmentVariable("AES_KEY");

        configuration["AESEncryption:IV"] =
            Environment.GetEnvironmentVariable("AES_IV");
    }

    private static void MapApp(IConfiguration configuration)
    {
        configuration["AppSettings:FrontendUrl"] =
            Environment.GetEnvironmentVariable("FRONTEND_URL");
    }
}