namespace LiveSessionService.Application.Features.Results.Playlists;

/// <summary>
/// Result model for playlist operations
/// </summary>
public sealed class PlaylistResult
{
    public Guid Id { get; init; }
    public Guid StationId { get; init; }
    public string PlaylistName { get; init; } = null!;
    public string? Description { get; init; }
    public bool IsAutoPlay { get; init; }
    public int TotalTracks { get; init; }
    public int TotalDuration { get; init; }
    public DateTime CreatedAt { get; init; }
}
