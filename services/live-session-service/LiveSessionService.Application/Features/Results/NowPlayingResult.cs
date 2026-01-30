namespace LiveSessionService.Application.Features.Results;

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

/// <summary>
/// Result for Song information
/// </summary>
public sealed class SongResult
{
    public string Title { get; init; } = null!;
    public string? Artist { get; init; }
    public string? Album { get; init; }
    public string? ArtUrl { get; init; }
    public int DurationSeconds { get; init; }
    public bool IsRequest { get; init; }
    public Guid? RequestedByUserId { get; init; }
}

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
