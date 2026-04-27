using AiService.Application.Interfaces;
using AiService.Application.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AiService.Infrastructure.Clients;

public class GeminiLlmClient : ILlmClient
{
    private const int MaxRetryAttempts = 3;
    private readonly HttpClient _http;
    private readonly LlmOptions _options;
    private readonly IGeminiRuntimeConfigProvider _runtimeConfigProvider;
    private readonly ILogger<GeminiLlmClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public GeminiLlmClient(
        HttpClient http,
        IOptions<LlmOptions> options,
        IGeminiRuntimeConfigProvider runtimeConfigProvider,
        ILogger<GeminiLlmClient> logger)
    {
        _http = http;
        _options = options.Value;
        _runtimeConfigProvider = runtimeConfigProvider;
        _logger = logger;
    }

    public async Task<LlmGenerateResponse> GenerateAsync(LlmGenerateRequest request, CancellationToken cancellationToken)
    {
        var apiKey = await _runtimeConfigProvider.GetActiveApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured. Set Llm__ApiKey/LLM_API_KEY or provide an active Gemini key in ai_service_configs.");
        }

        if (string.IsNullOrWhiteSpace(_options.BaseUrl) || _options.BaseUrl.StartsWith("${"))
        {
            throw new InvalidOperationException("LLM BaseUrl is not configured.");
        }

        var model = string.IsNullOrWhiteSpace(request.ModelName) ? (_options.Model ?? "gemini-2.5-flash") : request.ModelName;
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/v1beta/models/{model}:generateContent?key={apiKey}";

        var promptParts = new List<object>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            promptParts.Add(new { text = request.SystemPrompt });
        }
        promptParts.Add(new { text = request.InputText });

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = promptParts
                }
            },
            generationConfig = new
            {
                temperature = request.Temperature ?? 0.7m,
                maxOutputTokens = request.MaxTokens ?? 2048,
                topP = 0.95,
                topK = 40
            }
        };

        _logger.LogInformation("Calling Gemini API model {Model}", model);

        for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            using var response = await _http.PostAsJsonAsync(url, payload, JsonOptions, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(JsonOptions, cancellationToken);

                var parts = result?.Candidates?.FirstOrDefault()?.Content?.Parts;
                var generatedText = parts != null ? string.Join("", parts.Select(p => p.Text)) : null;

                if (string.IsNullOrWhiteSpace(generatedText))
                {
                    _logger.LogError("Gemini API returned empty content. Full response: {Raw}", JsonSerializer.Serialize(result));
                    throw new InvalidOperationException("Gemini API returned empty content.");
                }

                return new LlmGenerateResponse(
                    ContentText: generatedText,
                    TokensUsed: null, // Gemini REST doesn't always return this easily without extra parsing
                    RawProviderResponse: JsonSerializer.Serialize(result)
                );
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini API error {StatusCode}: {Body}", (int)response.StatusCode, errorBody);

            if (ShouldRetry(response.StatusCode) && attempt < MaxRetryAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(
                    "Transient Gemini error {StatusCode}. Retrying attempt {NextAttempt}/{MaxAttempts} after {DelaySeconds}s",
                    (int)response.StatusCode,
                    attempt + 1,
                    MaxRetryAttempts,
                    delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
                continue;
            }

            // Return client error (400) for invalid/unsupported model instead of generic 500.
            if (response.StatusCode == HttpStatusCode.NotFound &&
                errorBody.Contains("is not found", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Gemini model '{model}' is invalid or unsupported for generateContent.");
            }

            // Return client error (400) when Gemini rejects API key.
            if (response.StatusCode == HttpStatusCode.BadRequest &&
                (errorBody.Contains("API key not valid", StringComparison.OrdinalIgnoreCase) ||
                 errorBody.Contains("API_KEY_INVALID", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Gemini API key is invalid. Please configure a valid Gemini key in account-content-service.");
            }

            throw new InvalidOperationException($"Gemini API error: {response.StatusCode}. {errorBody}");
        }

        throw new InvalidOperationException("Gemini API request failed after all retry attempts.");
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.ServiceUnavailable
            || statusCode == HttpStatusCode.TooManyRequests
            || statusCode == HttpStatusCode.GatewayTimeout
            || statusCode == HttpStatusCode.RequestTimeout;
    }

    private class GeminiResponse
    {
        public GeminiCandidate[]? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    private class GeminiContent
    {
        public GeminiPart[]? Parts { get; set; }
    }

    private class GeminiPart
    {
        public string? Text { get; set; }
    }
}
