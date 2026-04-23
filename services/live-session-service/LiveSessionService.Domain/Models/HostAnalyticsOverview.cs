namespace LiveSessionService.Domain.Models;

public sealed class HostAnalyticsOverview
{
    public int TotalListeners { get; init; }
    public int TotalSessions { get; init; }
    public int PeakListeners { get; init; }
    public int TotalMusicRequests { get; init; }
    public List<DailyHostAnalyticsMetric> ChartData { get; init; } = [];
    public List<TopSongRequestMetric> TopRequestedSongs { get; init; } = [];
    public List<EndedSessionAnalysis> EndedSessionsAnalysis { get; init; } = [];
}

public sealed class DailyHostAnalyticsMetric
{
    public DateTime Date { get; init; }
    public int ListenersCount { get; init; }
    public int RequestsCount { get; init; }
    public int ChatCount { get; init; }
}

public sealed class TopSongRequestMetric
{
    public int Rank { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Artist { get; init; }
    public int Count { get; init; }
}
