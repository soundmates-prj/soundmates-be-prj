namespace LiveSessionService.Application.Features.Results.NowPlaying;

public sealed class LiveSessionQueueResult
{
    public Guid SessionId { get; init; }
    public int ExternalStationId { get; init; }
    public List<NowPlayingTrackResult> Queue { get; init; } = [];
}
