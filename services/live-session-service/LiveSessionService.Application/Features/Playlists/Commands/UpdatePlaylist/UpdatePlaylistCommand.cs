using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Playlists;

namespace LiveSessionService.Application.Features.Playlists.Commands.UpdatePlaylist;

public sealed record UpdatePlaylistCommand(
    Guid PlaylistId,
    string? PlaylistName,
    string? Description,
    bool? IsAutoPlay,
    bool? IncludeInRequests,
    bool? IncludeInOnDemand,
    bool? IsEnabled,
    string? SongPlaybackOrder) : ICommand<PlaylistResult>;
