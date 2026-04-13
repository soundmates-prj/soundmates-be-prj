namespace LiveSessionService.Application.Features.Results.NowPlaying;

public sealed class StationNowPlayingResult
{
    public int ExternalStationId { get; init; }
    public string StationName { get; init; } = null!;
    public string? StationShortcode { get; init; }
    public string? ListenUrl { get; init; }
    public string? PublicPlayerUrl { get; init; }
    public bool IsOnline { get; init; }
    public bool IsLive { get; init; }
    public string? StreamerName { get; init; }
    public int TotalListeners { get; init; }
    public int UniqueListeners { get; init; }
    public NowPlayingTrackResult? CurrentTrack { get; init; }
    public NowPlayingTrackResult? PlayingNext { get; init; }
    public List<NowPlayingTrackResult> SongHistory { get; init; } = [];
}

public sealed class NowPlayingTrackResult
{
    public long ShId { get; init; }
    public string? Text { get; init; }
    public string? Title { get; init; }
    public string? Artist { get; init; }
    public string? Album { get; init; }
    public string? Genre { get; init; }
    public string? ArtUrl { get; init; }
    public string? Lyrics { get; init; }
    public double PlayedAt { get; init; }
    public double? Duration { get; init; }
    public double Elapsed { get; init; }
    public double Remaining { get; init; }
    public bool IsRequest { get; init; }
}

