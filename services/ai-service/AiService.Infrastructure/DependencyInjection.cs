using AiService.Application.Interfaces;
using AiService.Domain.Interfaces;
using AiService.Infrastructure.Clients;
using AiService.Infrastructure.Persistence;
using AiService.Infrastructure.Repositories;
using AiService.Infrastructure.Storage;
using AiService.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AiService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AiDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IAiPromptRepository, AiPromptRepository>();
        services.AddScoped<IScriptRepository, ScriptRepository>();
        services.AddScoped<IVoiceRepository, VoiceRepository>();
        services.AddScoped<IScriptAudioRepository, ScriptAudioRepository>();
        services.AddScoped<IAiUsageRepository, AiUsageRepository>();

        services.AddSingleton<IAudioStorage, LocalAudioStorage>();

        services.AddHttpClient<ILlmClient, GeminiLlmClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<VieNeuTtsClient>((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<TtsOptions>>().Value;

            if (!string.IsNullOrWhiteSpace(options.BaseUrl)
                && !options.BaseUrl.StartsWith("${")
                && Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri))
            {
                http.BaseAddress = baseUri;
            }

            if (options.TimeoutSeconds > 0)
            {
                http.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            }
        });
        services.AddScoped<ITtsClient, VieNeuTtsClient>();

        services.AddHttpClient<IPodcastSyncClient, PodcastSyncClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<IPodcastPipelineService, PodcastPipelineService>();
        
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));
        services.Configure<TtsOptions>(configuration.GetSection("Tts"));
        services.Configure<LlmOptions>(configuration.GetSection("Llm"));

        return services;
    }
}

