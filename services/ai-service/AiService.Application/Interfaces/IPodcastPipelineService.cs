using AiService.Application.Results;
using AiService.Domain.Entities;

namespace AiService.Application.Interfaces;

public record GeneratePodcastRequest(
    Guid UserId,
    string Topic,
    string? Title,
    string ContextType,
    string VoiceCode,
    decimal? Speed = 1.0m,
    decimal? Pitch = 1.0m,
    string? ModelName = null);

public record PodcastExecutionResult(
    Script Script,
    ScriptAudio Audio,
    bool SyncedToLiveService,
    string? LiveServicePodcastId = null);

public interface IPodcastPipelineService
{
    Task<Result<PodcastExecutionResult>> GenerateAndSyncPodcastAsync(GeneratePodcastRequest request, CancellationToken ct);
}
