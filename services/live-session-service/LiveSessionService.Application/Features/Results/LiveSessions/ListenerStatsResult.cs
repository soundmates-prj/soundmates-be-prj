namespace LiveSessionService.Application.Features.Results.LiveSessions;

/// <summary>
/// Listener statistics result for live sessions
/// </summary>
public sealed class ListenerStatsResult
{
    public Guid SessionId { get; init; }
    public int CurrentListeners { get; init; }
    public int PeakListeners { get; init; }
    public int TotalListeners { get; init; }
}
