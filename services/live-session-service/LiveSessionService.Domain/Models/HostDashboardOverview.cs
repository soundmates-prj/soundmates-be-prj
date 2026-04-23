using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Domain.Models;

public sealed class HostDashboardOverview
{
    public int TotalSessions { get; init; }
    public int TotalListeners { get; init; }
    public int PendingMusicRequests { get; init; }
    public List<DailyHostStatMetric> ChartData { get; init; } = [];
    public List<SongRequest> RecentMusicRequests { get; init; } = [];
    public List<EndedSessionAnalysis> EndedSessionsAnalysis { get; init; } = [];
}

public sealed class DailyHostStatMetric
{
    public DateTime Date { get; init; }
    public int SessionsCount { get; init; }
    public int ListenersCount { get; init; }
}

public sealed class EndedSessionAnalysis
{
    public Guid SessionId { get; init; }
    public string SessionName { get; init; } = string.Empty;
    public DateTime? EndedAt { get; init; }
    public double TotalDurationMinutes { get; init; }
    public int TotalListeners { get; init; }
    public int MusicRequestsCount { get; init; }
}
