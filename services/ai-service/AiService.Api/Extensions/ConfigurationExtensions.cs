using DotNetEnv;

namespace AiService.Api.Extensions;

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

        configuration["Llm:Provider"] = Environment.GetEnvironmentVariable("Llm__Provider")
            ?? Environment.GetEnvironmentVariable("LLM_PROVIDER")
            ?? Environment.GetEnvironmentVariable("GEMINI_PROVIDER")
            ?? configuration["Llm:Provider"];

        configuration["Llm:BaseUrl"] = Environment.GetEnvironmentVariable("Llm__BaseUrl")
            ?? Environment.GetEnvironmentVariable("LLM_BASE_URL")
            ?? configuration["Llm:BaseUrl"];

        configuration["Llm:ApiKey"] = Environment.GetEnvironmentVariable("Llm__ApiKey")
            ?? Environment.GetEnvironmentVariable("LLM_API_KEY")
            ?? configuration["Llm:ApiKey"];

        configuration["Llm:Model"] = Environment.GetEnvironmentVariable("Llm__Model")
            ?? Environment.GetEnvironmentVariable("LLM_MODEL")
            ?? configuration["Llm:Model"];

        configuration["Tts:Provider"] = Environment.GetEnvironmentVariable("Tts__Provider")
            ?? Environment.GetEnvironmentVariable("TTS_PROVIDER")
            ?? configuration["Tts:Provider"];

        configuration["Tts:BaseUrl"] = Environment.GetEnvironmentVariable("Tts__BaseUrl")
            ?? Environment.GetEnvironmentVariable("TTS_BASE_URL")
            ?? configuration["Tts:BaseUrl"];

        configuration["Tts:ApiKey"] = Environment.GetEnvironmentVariable("Tts__ApiKey")
            ?? Environment.GetEnvironmentVariable("TTS_API_KEY")
            ?? configuration["Tts:ApiKey"];

        configuration["Tts:SynthesizePath"] = Environment.GetEnvironmentVariable("Tts__SynthesizePath")
            ?? Environment.GetEnvironmentVariable("TTS_SYNTHESIZE_PATH")
            ?? configuration["Tts:SynthesizePath"];

        configuration["Tts:AudioFormat"] = Environment.GetEnvironmentVariable("Tts__AudioFormat")
            ?? Environment.GetEnvironmentVariable("TTS_AUDIO_FORMAT")
            ?? configuration["Tts:AudioFormat"];

        configuration["Tts:PromptTemplate"] = Environment.GetEnvironmentVariable("Tts__PromptTemplate")
            ?? Environment.GetEnvironmentVariable("TTS_PROMPT_TEMPLATE")
            ?? configuration["Tts:PromptTemplate"];

        configuration["Tts:TimeoutSeconds"] = FirstPositiveIntString(
            Environment.GetEnvironmentVariable("Tts__TimeoutSeconds"),
            Environment.GetEnvironmentVariable("TTS_TIMEOUT_SECONDS"),
            configuration["Tts:TimeoutSeconds"])
            ?? "100";

        configuration["Tts:DemoMode"] = Environment.GetEnvironmentVariable("Tts__DemoMode")
            ?? Environment.GetEnvironmentVariable("TTS_DEMO_MODE")
            ?? configuration["Tts:DemoMode"];

        configuration["Storage:AudioRoot"] = Environment.GetEnvironmentVariable("Storage__AudioRoot")
            ?? Environment.GetEnvironmentVariable("AUDIO_STORAGE_ROOT")
            ?? configuration["Storage:AudioRoot"];

        configuration["Storage:PublicBaseUrl"] = Environment.GetEnvironmentVariable("Storage__PublicBaseUrl")
            ?? Environment.GetEnvironmentVariable("PUBLIC_BASE_URL")
            ?? configuration["Storage:PublicBaseUrl"];
    }

    private static string? FirstPositiveIntString(params string?[] candidates)
    {
        foreach (var raw in candidates)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var trimmed = raw.Trim();
            if (trimmed.StartsWith("${", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
                continue;

            if (int.TryParse(trimmed, out var value) && value > 0)
                return value.ToString();
        }

        return null;
    }
}

