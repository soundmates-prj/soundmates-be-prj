using LiveSessionService.Application.Features.Results.SongRequests;

namespace LiveSessionService.Application.Features.Results.LiveSessions;

public sealed class HostDashboardOverviewResult
{
    public int TotalSessions { get; init; }
    public int TotalListeners { get; init; }
    public int PendingMusicRequests { get; init; }
    public List<DailyHostStatResult> ChartData { get; init; } = [];
    public List<SongRequestResult> RecentMusicRequests { get; init; } = [];
    public List<SessionScheduleResult> UpcomingSchedules { get; init; } = [];
    public List<EndedSessionAnalysisResult> EndedSessionsAnalysis { get; init; } = [];
}

public sealed class DailyHostStatResult
{
    public string Date { get; init; } = string.Empty;
    public int SessionsCount { get; init; }
    public int ListenersCount { get; init; }
}

public sealed class EndedSessionAnalysisResult
{
    public Guid SessionId { get; init; }
    public string SessionName { get; init; } = string.Empty;
    public DateTime? EndedAt { get; init; }
    public double TotalDurationMinutes { get; init; }
    public int TotalListeners { get; init; }
    public int MusicRequestsCount { get; init; }
}
