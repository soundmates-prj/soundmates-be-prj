using DotNetEnv;

namespace AiService.Api.Extensions;

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
            // ignore
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
                    var cs =
                        $"Host={host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Disable;Trust Server Certificate=True;";
                    configuration["ConnectionStrings:DefaultConnection"] = cs;
                }
            }
            else if (key == "JWT_KEY")
                configuration["Jwt:Key"] = value;
            else if (key == "JWT_ISSUER")
                configuration["Jwt:Issuer"] = value;
            else if (key == "JWT_AUDIENCE")
                configuration["Jwt:Audience"] = value;
            else if (key == "LLM_PROVIDER")
                configuration["Llm:Provider"] = value;
            else if (key == "LLM_BASE_URL")
                configuration["Llm:BaseUrl"] = value;
            else if (key == "LLM_API_KEY")
                configuration["Llm:ApiKey"] = value;
            else if (key == "LLM_MODEL")
                configuration["Llm:Model"] = value;
            else if (key == "TTS_PROVIDER")
                configuration["Tts:Provider"] = value;
            else if (key == "TTS_BASE_URL")
                configuration["Tts:BaseUrl"] = value;
            else if (key == "TTS_API_KEY")
                configuration["Tts:ApiKey"] = value;
            else if (key == "TTS_MODEL")
                configuration["Tts:Model"] = value;
            else if (key == "TTS_SYNTHESIZE_PATH")
                configuration["Tts:SynthesizePath"] = value;
            else if (key == "TTS_AUDIO_FORMAT")
                configuration["Tts:AudioFormat"] = value;
            else if (key == "TTS_PROMPT_TEMPLATE")
                configuration["Tts:PromptTemplate"] = value;
            else if (key == "TTS_TIMEOUT_SECONDS")
                configuration["Tts:TimeoutSeconds"] = value;
            else if (key == "AUDIO_STORAGE_ROOT")
                configuration["Storage:AudioRoot"] = value;
            else if (key == "PUBLIC_BASE_URL")
                configuration["Storage:PublicBaseUrl"] = value;
        }
    }
}

