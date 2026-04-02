using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Application.Features.Results.Playlists;

public sealed class UserPlaylistResult
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string PlaylistName { get; init; } = null!;
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public PlaylistVisibility Visibility { get; init; }
    public bool IsEnabled { get; init; }
    public int TotalTracks { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
