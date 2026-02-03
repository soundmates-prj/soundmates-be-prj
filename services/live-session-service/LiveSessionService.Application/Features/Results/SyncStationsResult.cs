namespace LiveSessionService.Application.Features.Results;

/// <summary>
/// Result for station sync operation
/// </summary>
public sealed class SyncStationsResult
{
    public int TotalStations { get; set; }
    public int CreatedStations { get; set; }
    public int UpdatedStations { get; set; }
    public int FailedStations { get; set; }
    public List<string> Errors { get; set; } = new();
}
