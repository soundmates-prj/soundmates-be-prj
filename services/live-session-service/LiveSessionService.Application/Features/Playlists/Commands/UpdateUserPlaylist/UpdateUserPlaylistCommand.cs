using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Playlists;

namespace LiveSessionService.Application.Features.Playlists.Commands.UpdateUserPlaylist;

public sealed record UpdateUserPlaylistCommand(
    Guid PlaylistId,
    Guid UserId,
    string? PlaylistName,
    bool? IncludeInRequests,
    bool? IncludeInOnDemand,
    bool? IsEnabled,
    int? PlaylistOrder,
    int? Weight) : ICommand<UserPlaylistResult>;
