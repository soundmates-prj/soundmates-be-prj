using AiService.Application.Interfaces;
using AiService.Domain.Interfaces;
using AiService.Infrastructure.Clients;
using AiService.Infrastructure.Persistence;
using AiService.Infrastructure.Repositories;
using AiService.Infrastructure.Messaging;
using AiService.Infrastructure.Services;
using AiService.Infrastructure.Storage;
using AiService.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System.Net;

namespace AiService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.AddDbContext<AiDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IAiPromptRepository, AiPromptRepository>();
        services.AddScoped<IAiServiceConfigRepository, AiServiceConfigRepository>();
        services.AddScoped<IScriptRepository, ScriptRepository>();
        services.AddScoped<IVoiceRepository, VoiceRepository>();
        services.AddScoped<IScriptAudioRepository, ScriptAudioRepository>();
        services.AddScoped<IAiUsageRepository, AiUsageRepository>();
        services.AddSingleton<IGeminiRuntimeConfigProvider, GeminiRuntimeConfigProvider>();

        services.AddSingleton<IAudioStorage, LocalAudioStorage>();

        IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy() =>
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30));

        services.AddHttpClient<ILlmClient, GeminiLlmClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        })
        .AddPolicyHandler(CreateCircuitBreakerPolicy());

        services.AddHttpClient<ITtsClient, VieNeuTtsClient>((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<TtsOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<VieNeuTtsClient>>();

            if (!string.IsNullOrWhiteSpace(options.BaseUrl)
                && !options.BaseUrl.StartsWith("${")
                && Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri))
            {
                http.BaseAddress = baseUri;
            }

            var timeoutSeconds = options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 100;
            http.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            logger.LogInformation(
                "Configured VieNeu HttpClient with BaseUrl={BaseUrl}, SynthesizePath={SynthesizePath}, TimeoutSeconds={TimeoutSeconds}",
                options.BaseUrl,
                options.SynthesizePath,
                timeoutSeconds);
        })
        .AddPolicyHandler(CreateCircuitBreakerPolicy());
        services.AddScoped<ITextToSpeechService, VieneuTextToSpeechService>();

        services.AddHttpClient<IPodcastSyncClient, PodcastSyncClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(CreateCircuitBreakerPolicy());

        services.AddScoped<IPodcastPipelineService, PodcastPipelineService>();
        
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));
        services.Configure<TtsOptions>(configuration.GetSection("Tts"));
        services.Configure<LlmOptions>(configuration.GetSection("Llm"));
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));

        services.AddHostedService<GeminiConfigConsumer>();

        return services;
    }
}

