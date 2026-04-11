using System.Linq;
using System.Text.RegularExpressions;
using AiService.Application.Constants;
using AiService.Application.Enums;
using AiService.Application.Interfaces;
using AiService.Application.Results;
using AiService.Domain.Interfaces;

namespace AiService.Application.Services;

public class GeminiService : IGeminiService
{
    private readonly IAiServiceConfigRepository _configRepository;
    private readonly ILlmClient _llmClient;

    public GeminiService(
        IAiServiceConfigRepository configRepository,
        ILlmClient llmClient)
    {
        _configRepository = configRepository;
        _llmClient = llmClient;
    }

    public async Task<Result<string>> GeneratePodcastScriptAsync(
        GeneratePodcastScriptFlowRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            return Result<string>.Failure("topic is required", (int)ApiStatusCode.HB40001);
        }

        var config = await _configRepository.GetActiveByProviderAsync(AiProviderConstants.Gemini, cancellationToken);
        if (config is null || string.IsNullOrWhiteSpace(config.ApiKey))
        {
            return Result<string>.Failure("Gemini API key is not configured", (int)ApiStatusCode.HB40401);
        }

        var template = string.IsNullOrWhiteSpace(config.PromptTemplate)
            ? PodcastPromptTemplateDefaults.ScriptTemplate
            : config.PromptTemplate;

        var systemPrompt = BuildSystemPrompt(template, request);

        // Pass null if no model specified — GeminiLlmClient will use LLM_MODEL from .env (gemini-2.5-flash).
        // Only override if the request explicitly provides a model name.
        var modelToUse = !string.IsNullOrWhiteSpace(request.ModelName) && request.ModelName.Contains("gemini", StringComparison.OrdinalIgnoreCase)
            ? request.ModelName.Trim()
            : null;

        var llmResponse = await _llmClient.GenerateAsync(
            new LlmGenerateRequest(
                InputText: request.Topic.Trim(),
                ModelName: modelToUse,
                Temperature: 0.7m,
                MaxTokens: 2048,
                ContextType: "podcast",
                SystemPrompt: systemPrompt),
            cancellationToken);

        var cleanedScript = CleanScriptForTts(llmResponse.ContentText);
        if (string.IsNullOrWhiteSpace(cleanedScript))
        {
            return Result<string>.Failure("Generated script is empty", (int)ApiStatusCode.HB50001);
        }

        return Result<string>.Success(cleanedScript);
    }

    private static string BuildSystemPrompt(string template, GeneratePodcastScriptFlowRequest request)
    {
        return template
            .Replace("{topic}", request.Topic.Trim(), StringComparison.OrdinalIgnoreCase)
            .Replace("{style}", string.IsNullOrWhiteSpace(request.Style) ? "tu nhien, de nghe" : request.Style.Trim(), StringComparison.OrdinalIgnoreCase)
            .Replace("{duration}", string.IsNullOrWhiteSpace(request.Duration) ? "5-7 phut" : request.Duration.Trim(), StringComparison.OrdinalIgnoreCase)
            .Replace("{language}", string.IsNullOrWhiteSpace(request.Language) ? "Vietnamese" : request.Language.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string CleanScriptForTts(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var cleaned = raw.Trim();

        // Loai bo block markdown va ky tu danh dau de TTS doc tu nhien hon.
        cleaned = Regex.Replace(cleaned, "```[\\s\\S]*?```", string.Empty, RegexOptions.Multiline);
        cleaned = Regex.Replace(cleaned, "^#{1,6}\\s*", string.Empty, RegexOptions.Multiline);
        cleaned = Regex.Replace(cleaned, "^[-*+]\\s+", string.Empty, RegexOptions.Multiline);
        cleaned = Regex.Replace(cleaned, "^\\d+\\.\\s+", string.Empty, RegexOptions.Multiline);

        return cleaned.Trim();
    }
}
