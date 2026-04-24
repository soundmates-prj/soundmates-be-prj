using LiveSessionService.Domain.Models;

namespace LiveSessionService.Application.Features.Results.LiveSessions;

public sealed class AdminAnalyticsOverviewResult
{
    public int TotalSessions { get; init; }
    public int TotalViews { get; init; }
    public int TotalInteractions { get; init; }

    public List<DailySessionMetric> SessionGrowthChart { get; init; } = new();
    public List<DailyListenerMetric> ListenerGrowthChart { get; init; } = new();
    public List<DailyInteractionMetric> InteractionGrowthChart { get; init; } = new();
}
