namespace LiveSessionService.Application.Abstractions;

public record AzuraCastPodcastUploadResult(
    bool IsSuccess,
    string? MediaId,
    string? ErrorMessage);

public interface IAzuraCastPodcastService
{
    /// <summary>
    /// Downloads audio from ai-service URL, converts .wav → .mp3,
    /// uploads to AzuraCast station storage, and queues it for playback.
    /// </summary>
    Task<AzuraCastPodcastUploadResult> UploadAndQueuePodcastAsync(
        int stationId,
        string audioUrl,
        string title,
        CancellationToken cancellationToken = default);
}