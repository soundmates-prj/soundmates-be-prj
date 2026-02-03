namespace LiveSessionService.Application.Features.Results.Stations;

/// <summary>
/// Result for station information
/// </summary>
public sealed class StationResult
{
    public Guid Id { get; init; }
    public int ExternalStationId { get; init; }
    public string StationName { get; init; } = null!;
    public string? StationShortcode { get; init; }
    public string? Description { get; init; }
    public string StreamUrl { get; init; } = null!;
    public string? PublicPlayerUrl { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime? LastSyncedAt { get; init; }
    public string SyncStatus { get; init; } = null!;
}
