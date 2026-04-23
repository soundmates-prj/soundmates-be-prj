namespace LiveSessionService.Application.Features.Results.LiveSessions;

public sealed class HostAnalyticsOverviewResult
{
    public int TotalListeners { get; init; }
    public int TotalSessions { get; init; }
    public int PeakListeners { get; init; }
    public int TotalMusicRequests { get; init; }
    public List<DailyHostAnalyticsResult> ChartData { get; init; } = [];
    public List<TopSongRequestResult> TopRequestedSongs { get; init; } = [];
    public List<EndedSessionAnalysisResult> EndedSessionsAnalysis { get; init; } = [];
}

public sealed class DailyHostAnalyticsResult
{
    public string Date { get; init; } = string.Empty;
    public int ListenersCount { get; init; }
    public int RequestsCount { get; init; }
    public int ChatCount { get; init; }
}

public sealed class TopSongRequestResult
{
    public int Rank { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Artist { get; init; }
    public int Count { get; init; }
}
