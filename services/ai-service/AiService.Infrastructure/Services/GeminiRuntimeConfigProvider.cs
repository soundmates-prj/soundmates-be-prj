using AiService.Application.Constants;
using AiService.Application.Interfaces;
using AiService.Infrastructure.Clients;
using AiService.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiService.Infrastructure.Services;

public sealed class GeminiRuntimeConfigProvider : IGeminiRuntimeConfigProvider
{
    private const string CacheKey = "gemini:active:api-key";

    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LlmOptions _llmOptions;
    private readonly ILogger<GeminiRuntimeConfigProvider> _logger;

    public GeminiRuntimeConfigProvider(
        IMemoryCache cache,
        IServiceScopeFactory scopeFactory,
        IOptions<LlmOptions> llmOptions,
        ILogger<GeminiRuntimeConfigProvider> logger)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
        _llmOptions = llmOptions.Value;
        _logger = logger;
    }

    public async Task<string?> GetActiveApiKeyAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue<string>(CacheKey, out var apiKey) && !string.IsNullOrWhiteSpace(apiKey))
        {
            return apiKey;
        }

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAiServiceConfigRepository>();
        var config = await repo.GetActiveByProviderAsync(AiProviderConstants.Gemini, cancellationToken);

        if (config is not null && config.IsActive && IsUsableApiKey(config.ApiKey))
        {
            _cache.Set(CacheKey, config.ApiKey, TimeSpan.FromHours(6));
            return config.ApiKey;
        }

        if (IsUsableApiKey(_llmOptions.ApiKey))
        {
            _logger.LogWarning("Gemini runtime key not found in ai_service_configs. Falling back to Llm:ApiKey from environment/config.");
            _cache.Set(CacheKey, _llmOptions.ApiKey!, TimeSpan.FromMinutes(30));
            return _llmOptions.ApiKey;
        }

        return null;
    }

    public async Task UpdateAsync(
        string provider,
        string apiKey,
        bool isActive,
        DateTime updatedAt,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(provider, AiProviderConstants.Gemini, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (isActive && !string.IsNullOrWhiteSpace(apiKey))
        {
            _cache.Set(CacheKey, apiKey, TimeSpan.FromHours(6));
        }
        else
        {
            _cache.Remove(CacheKey);
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAiServiceConfigRepository>();
            await repo.UpsertAsync(provider, apiKey, isActive, updatedAt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist Gemini runtime config update.");
        }
    }

    private static bool IsUsableApiKey(string? apiKey)
    {
        return !string.IsNullOrWhiteSpace(apiKey)
            && !apiKey.StartsWith("${", StringComparison.Ordinal)
            && !apiKey.Contains("<", StringComparison.Ordinal);
    }
}