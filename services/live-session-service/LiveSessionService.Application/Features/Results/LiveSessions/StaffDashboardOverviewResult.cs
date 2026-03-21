namespace LiveSessionService.Application.Features.Results.LiveSessions;

public sealed class StaffDashboardOverviewResult
{
    public int TotalStations { get; init; }
    public int StationsCreatedToday { get; init; }
    public int TotalSessions { get; init; }
    public int LiveSessions { get; init; }
    public int ListenersToday { get; init; }
    public List<DailyListenerPointResult> DailyListeners { get; init; } = [];
}

public sealed class DailyListenerPointResult
{
    public string Date { get; init; } = string.Empty;
    public int ListenerCount { get; init; }
}
