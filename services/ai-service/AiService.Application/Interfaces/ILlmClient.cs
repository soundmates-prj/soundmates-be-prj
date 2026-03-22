namespace AiService.Application.Interfaces;

public record LlmGenerateRequest(
    string InputText,
    string? ModelName,
    decimal? Temperature,
    int? MaxTokens,
    string ContextType,
    string? SystemPrompt = null);

public record LlmGenerateResponse(
    string ContentText,
    int? TokensUsed = null,
    decimal? Cost = null,
    string? RawProviderResponse = null);

public interface ILlmClient
{
    Task<LlmGenerateResponse> GenerateAsync(LlmGenerateRequest request, CancellationToken cancellationToken);
}

