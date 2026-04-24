namespace LiveSessionService.Application.Features.Results.LiveSessions;

public sealed class StaffAnalyticsOverviewResult
{
    public int TotalSystemMusic { get; init; }
    public long TotalStorageBytes { get; init; }
    public int TotalStations { get; init; }
    public int PendingSongRequests { get; init; }
    
    public List<DailyContentGrowthResult> ContentGrowthChart { get; init; } = new();
    public List<DailyModerationResult> ModerationChart { get; init; } = new();
}

public sealed class DailyContentGrowthResult
{
    public string Date { get; init; } = string.Empty;
    public int NewMusicCount { get; init; }
}

public sealed class DailyModerationResult
{
    public string Date { get; init; } = string.Empty;
    public int PendingCount { get; init; }
    public int ResolvedCount { get; init; }
}

public sealed class PendingRequestResult
{
    public Guid RequestId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string RequestedBy { get; init; } = string.Empty;
    public DateTime RequestedAt { get; init; }
}
