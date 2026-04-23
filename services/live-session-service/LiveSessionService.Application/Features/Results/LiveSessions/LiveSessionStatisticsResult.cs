namespace LiveSessionService.Application.Features.Results.LiveSessions;

public sealed class LiveSessionStatisticsResult
{
    public Guid LiveSessionId { get; init; }
    public string SessionName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? StationId { get; init; }
    public string? StationName { get; init; }
    public Guid HostUserId { get; init; }
    public string? HostName { get; init; }
    public string Status { get; init; } = string.Empty;

    public DateOnly? StartDate { get; init; }
    public TimeOnly? StartTime { get; init; }
    public DateOnly? EndDate { get; init; }
    public TimeOnly? EndTime { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public int TotalDurationSeconds { get; init; }

    public int UniqueListeners { get; init; }
    public int PeakConcurrentListeners { get; init; }
    public int TotalMessages { get; init; }

    public List<PlayedSongReportItemResult> SongsPlayed { get; init; } = [];
    public int TotalSongRequests { get; init; }
    public List<SongRequestCountReportItemResult> TopRequestedSongs { get; init; } = [];

    public int AcceptedRequests { get; init; }
    public int RejectedRequests { get; init; }
}

public sealed class PlayedSongReportItemResult
{
    public Guid Id { get; init; }
    public string SongTitle { get; init; } = string.Empty;
    public string? SongArtist { get; init; }
    public DateTime PlayedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public bool IsRequest { get; init; }
}

public sealed class SongRequestCountReportItemResult
{
    public Guid MediaFileId { get; init; }
    public string SongTitle { get; init; } = string.Empty;
    public string? SongArtist { get; init; }
    public int RequestCount { get; init; }
}
