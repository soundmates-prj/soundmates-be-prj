namespace LiveSessionService.Application.Features.Results.NowPlaying;

/// <summary>
/// Result for Now Playing History item
/// </summary>
public sealed class NowPlayingHistoryResult
{
    public Guid Id { get; init; }
    public SongResult Song { get; init; } = null!;
    public DateTime PlayedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public int ListenerPeak { get; init; }
    public int DurationSeconds { get; init; }
}
