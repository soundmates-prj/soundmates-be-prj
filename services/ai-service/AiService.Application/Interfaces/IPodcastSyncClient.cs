namespace AiService.Application.Interfaces;

public record PodcastSyncRequest(
    Guid CreatedBy,
    string Title,
    string? Description,
    string? Author,
    string? Type,
    string? Banner,
    string AudioUrl);

public record PodcastSyncResponse(
    bool Success,
    string? PodcastId,
    string? ErrorMessage);

public interface IPodcastSyncClient
{
    Task<PodcastSyncResponse> SyncToLiveServiceAsync(PodcastSyncRequest request, CancellationToken ct);
}
