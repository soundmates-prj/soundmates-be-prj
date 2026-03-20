using AiService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace AiService.Infrastructure.Clients;

public class GeminiLlmClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly LlmOptions _options;
    private readonly ILogger<GeminiLlmClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public GeminiLlmClient(HttpClient http, IOptions<LlmOptions> options, ILogger<GeminiLlmClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<LlmGenerateResponse> GenerateAsync(LlmGenerateRequest request, CancellationToken cancellationToken)
    {
        var apiKey = _options.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("${"))
        {
            throw new InvalidOperationException("Gemini API Key is not configured.");
        }

        var model = string.IsNullOrWhiteSpace(request.ModelName) ? (_options.Model ?? "gemini-2.5-flash") : request.ModelName;
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = BuildSystemPrompt(request) },
                        new { text = request.InputText }
                    }
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

        using var response = await _http.PostAsJsonAsync(url, payload, JsonOptions, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini API error {StatusCode}: {Body}", (int)response.StatusCode, errorBody);

            // Return client error (400) for invalid/unsupported model instead of generic 500.
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound &&
                errorBody.Contains("is not found", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Gemini model '{model}' is invalid or unsupported for generateContent.");
            }

            // Return client error (400) when Gemini rejects API key.
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                (errorBody.Contains("API key not valid", StringComparison.OrdinalIgnoreCase) ||
                 errorBody.Contains("API_KEY_INVALID", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Gemini API key is invalid. Please configure a valid LLM_API_KEY.");
            }

            throw new InvalidOperationException($"Gemini API error: {response.StatusCode}. {errorBody}");
        }

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

    private string BuildSystemPrompt(LlmGenerateRequest request)
    {
        if (string.Equals(request.ContextType, "podcast", StringComparison.OrdinalIgnoreCase))
        {
            return "Bạn là một người viết kịch bản podcast chuyên nghiệp. Hãy viết một kịch bản podcast hấp dẫn, bằng tiếng Việt, dựa trên chủ đề người dùng cung cấp. Kịch bản nên bao gồm lời dẫn (Intro), nội dung chính (Body) và lời kết (Outro). Hãy viết theo phong cách tự nhiên, dễ nghe.";
        }
        
        return "Bạn là một trợ lý AI thông minh. Hãy trả lời yêu cầu sau bằng tiếng Việt.";
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
