namespace LiveSessionService.Application.Features.Results.Playlists;

public sealed class UserPlaylistResult
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string PlaylistName { get; init; } = null!;
    public bool IsEnabled { get; init; }
    public bool IncludeInRequests { get; init; }
    public bool IncludeInOnDemand { get; init; }
    public int PlaylistOrder { get; init; }
    public int Weight { get; init; }
    public int TotalTracks { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
