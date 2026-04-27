namespace LiveSessionService.Domain.Models;

public sealed class StaffDashboardOverview
{
    public int TotalSessions { get; init; }
    public int LiveSessions { get; init; }
    public int ListenersToday { get; init; }
    public List<DailyListenerMetric> DailyListeners { get; init; } = [];
}

public sealed class DailyListenerMetric
{
    public DateTime Date { get; init; }
    public int ListenerCount { get; init; }
}