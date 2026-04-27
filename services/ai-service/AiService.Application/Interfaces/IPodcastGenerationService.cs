using AiService.Application.Results;

namespace AiService.Application.Interfaces;

public record PodcastGenerateRequest(
    Guid UserId,
    string Topic,
    string? Style,
    string? Duration,
    string Voice,
    string? Language,
    string? ModelName,
    bool IncludeAudioBytes = false);

public record PodcastGenerateResult(
    string Script,
    string? AudioUrl,
    byte[]? AudioBytes,
    int? Duration,
    string Provider);

public interface IPodcastGenerationService
{
    Task<Result<PodcastGenerateResult>> GeneratePodcastAudioAsync(PodcastGenerateRequest request, CancellationToken cancellationToken);
}
