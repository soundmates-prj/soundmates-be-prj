namespace LiveSessionService.Domain.Models;

public class StaffAnalyticsOverview
{
    public int TotalSystemMusic { get; set; }
    public long TotalStorageBytes { get; set; }
    public int TotalStations { get; set; }
    public int PendingSongRequests { get; set; }
    
    public List<DailyContentGrowthMetric> ContentGrowthChart { get; set; } = new();
    public List<DailyModerationMetric> ModerationChart { get; set; } = new();
    public List<PendingRequestMetric> ActionRequiredList { get; set; } = new();
}

public class DailyContentGrowthMetric
{
    public DateTime Date { get; set; }
    public int NewMusicCount { get; set; }
}

public class DailyModerationMetric
{
    public DateTime Date { get; set; }
    public int PendingCount { get; set; }
    public int ResolvedCount { get; set; }
}

public class PendingRequestMetric
{
    public Guid RequestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "music"
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
}
