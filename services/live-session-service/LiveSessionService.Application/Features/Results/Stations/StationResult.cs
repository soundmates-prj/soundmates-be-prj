namespace LiveSessionService.Application.Features.Results.Stations;

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
    public DateTime CreatedAt { get; init; }
    public DateTime? LastSyncedAt { get; init; }
    public string SyncStatus { get; init; } = null!;
    public List<MountResult> Mounts { get; init; } = [];
}

public sealed class MountResult
{
    public int ExternalMountId { get; init; }
    public string MountName { get; init; } = null!;
    public string MountPath { get; init; } = null!;
    public string? MountUrl { get; init; }
    public bool IsDefault { get; init; }
    public int? Bitrate { get; init; }
    public string? Format { get; init; }
    public int? CurrentListeners { get; init; }
}
