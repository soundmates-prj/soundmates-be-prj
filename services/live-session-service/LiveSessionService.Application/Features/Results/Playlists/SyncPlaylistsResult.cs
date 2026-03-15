namespace LiveSessionService.Application.Features.Results.Playlists;

/// <summary>
/// Result for sync playlists operation
/// </summary>
public sealed class SyncPlaylistsResult
{
    public Guid StationId { get; init; }
    public string StationName { get; init; } = null!;
    public int TotalPlaylistsInAzuraCast { get; init; }
    public int NewPlaylistsSynced { get; init; }
    public int ExistingPlaylists { get; init; }
    public int TotalMediaFilesSynced { get; init; }
    public List<PlaylistResult> Playlists { get; init; } = new();
}
