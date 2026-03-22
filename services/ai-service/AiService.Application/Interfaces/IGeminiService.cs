using AiService.Application.Results;

namespace AiService.Application.Interfaces;

public record GeneratePodcastScriptFlowRequest(
    string Topic,
    string? Style,
    string? Duration,
    string? Language,
    string? ModelName);

public interface IGeminiService
{
    Task<Result<string>> GeneratePodcastScriptAsync(GeneratePodcastScriptFlowRequest request, CancellationToken cancellationToken);
}
