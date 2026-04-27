namespace LiveSessionService.Application.Features.Results.NowPlaying;

/// <summary>
/// Result for Now Playing operations
/// Pure data contract - no Domain dependencies
/// </summary>
public sealed class NowPlayingResult
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public string SessionName { get; init; } = null!;
    public SongResult CurrentSong { get; init; } = null!;
    public int ListenerCount { get; init; }
    public int ListenerPeak { get; init; }
    public DateTime PlayedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public bool IsLive { get; init; }
    public DateTime SyncedAt { get; init; }
}
