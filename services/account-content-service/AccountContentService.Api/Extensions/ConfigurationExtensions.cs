using DotNetEnv;

namespace AccountContentService.Api.Extensions;

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
        var secret = Environment.GetEnvironmentVariable("Jwt__Secret")
            ?? Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? Environment.GetEnvironmentVariable("JWT_KEY");
        if (!string.IsNullOrWhiteSpace(secret))
        {
            configuration["Jwt:Secret"] = secret;
            configuration["Jwt:Key"] = secret;
        }

        var issuer = Environment.GetEnvironmentVariable("Jwt__Issuer")
            ?? Environment.GetEnvironmentVariable("JWT_ISSUER");
        if (!string.IsNullOrWhiteSpace(issuer))
            configuration["Jwt:Issuer"] = issuer;

        var audience = Environment.GetEnvironmentVariable("Jwt__Audience")
            ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE");
        if (!string.IsNullOrWhiteSpace(audience))
            configuration["Jwt:Audience"] = audience;

        var expiresInHours = Environment.GetEnvironmentVariable("Jwt__ExpiresInHours")
            ?? Environment.GetEnvironmentVariable("JWT_EXPIRES_IN_HOURS");
        if (!string.IsNullOrWhiteSpace(expiresInHours))
            configuration["Jwt:ExpiresInHours"] = expiresInHours;

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            configuration["ConnectionStrings:DefaultConnection"] = connectionString;
        }
        else
        {
            var host = Environment.GetEnvironmentVariable("DB_HOST") ?? Environment.GetEnvironmentVariable("POSTGRES_HOST");
            var port = Environment.GetEnvironmentVariable("DB_PORT") ?? Environment.GetEnvironmentVariable("POSTGRES_PORT");
            var database = Environment.GetEnvironmentVariable("ACCOUNT_CONTENT_DB_NAME")
                ?? Environment.GetEnvironmentVariable("DB_NAME")
                ?? Environment.GetEnvironmentVariable("POSTGRES_DATABASE");
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

        configuration["GoogleOAuth:ClientId"] = Environment.GetEnvironmentVariable("GoogleOAuth__ClientId")
            ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
            ?? configuration["GoogleOAuth:ClientId"];

        configuration["GoogleOAuth:ClientSecret"] = Environment.GetEnvironmentVariable("GoogleOAuth__ClientSecret")
            ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
            ?? configuration["GoogleOAuth:ClientSecret"];

        configuration["EmailSettings:Host"] = Environment.GetEnvironmentVariable("EmailSettings__Host")
            ?? Environment.GetEnvironmentVariable("EMAIL_HOST")
            ?? configuration["EmailSettings:Host"];

        configuration["EmailSettings:Port"] = Environment.GetEnvironmentVariable("EmailSettings__Port")
            ?? Environment.GetEnvironmentVariable("EMAIL_PORT")
            ?? configuration["EmailSettings:Port"];

        configuration["EmailSettings:From"] = Environment.GetEnvironmentVariable("EmailSettings__From")
            ?? Environment.GetEnvironmentVariable("EMAIL_FROM")
            ?? configuration["EmailSettings:From"];

        configuration["EmailSettings:Username"] = Environment.GetEnvironmentVariable("EmailSettings__Username")
            ?? Environment.GetEnvironmentVariable("EMAIL_USERNAME")
            ?? configuration["EmailSettings:Username"];

        configuration["EmailSettings:Password"] = Environment.GetEnvironmentVariable("EmailSettings__Password")
            ?? Environment.GetEnvironmentVariable("EMAIL_PASSWORD")
            ?? configuration["EmailSettings:Password"];

        configuration["VNPay:TmnCode"] = Environment.GetEnvironmentVariable("VNPay__TmnCode")
            ?? Environment.GetEnvironmentVariable("VNPAY_TMN_CODE")
            ?? configuration["VNPay:TmnCode"];

        configuration["VNPay:HashSecret"] = Environment.GetEnvironmentVariable("VNPay__HashSecret")
            ?? Environment.GetEnvironmentVariable("VNPAY_HASH_SECRET")
            ?? configuration["VNPay:HashSecret"];

        configuration["VNPay:BaseUrl"] = Environment.GetEnvironmentVariable("VNPay__BaseUrl")
            ?? Environment.GetEnvironmentVariable("VNPAY_BASE_URL")
            ?? configuration["VNPay:BaseUrl"];

        configuration["VNPay:ReturnUrl"] = Environment.GetEnvironmentVariable("VNPay__ReturnUrl")
            ?? Environment.GetEnvironmentVariable("VNPAY_RETURN_URL")
            ?? configuration["VNPay:ReturnUrl"];

        configuration["AESEncryption:Key"] = Environment.GetEnvironmentVariable("AESEncryption__Key")
            ?? Environment.GetEnvironmentVariable("AES_KEY")
            ?? configuration["AESEncryption:Key"];

        configuration["AESEncryption:IV"] = Environment.GetEnvironmentVariable("AESEncryption__IV")
            ?? Environment.GetEnvironmentVariable("AES_IV")
            ?? configuration["AESEncryption:IV"];

        configuration["AppSettings:FrontendUrl"] = Environment.GetEnvironmentVariable("AppSettings__FrontendUrl")
            ?? Environment.GetEnvironmentVariable("FRONTEND_URL")
            ?? configuration["AppSettings:FrontendUrl"];

        configuration["AppSettings:BaseUrl"] = Environment.GetEnvironmentVariable("AppSettings__BaseUrl")
            ?? Environment.GetEnvironmentVariable("BASE_URL")
            ?? configuration["AppSettings:BaseUrl"];

        configuration["AppSettings:ApiBaseUrl"] = Environment.GetEnvironmentVariable("AppSettings__ApiBaseUrl")
            ?? Environment.GetEnvironmentVariable("API_BASE_URL")
            ?? Environment.GetEnvironmentVariable("BASE_URL")
            ?? configuration["AppSettings:ApiBaseUrl"];
    }
}