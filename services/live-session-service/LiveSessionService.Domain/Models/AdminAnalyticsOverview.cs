namespace LiveSessionService.Domain.Models;

public class AdminAnalyticsOverview
{
    public int TotalSessions { get; set; }
    public int TotalViews { get; set; }
    public int TotalInteractions { get; set; }

    public List<DailySessionMetric> SessionGrowthChart { get; set; } = new();
    public List<DailyListenerMetric> ListenerGrowthChart { get; set; } = new();
    public List<DailyInteractionMetric> InteractionGrowthChart { get; set; } = new();
}

public class DailySessionMetric
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class DailyInteractionMetric
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}
