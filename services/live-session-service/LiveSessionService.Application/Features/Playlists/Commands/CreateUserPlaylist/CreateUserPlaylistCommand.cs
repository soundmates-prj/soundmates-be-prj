using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Playlists;

namespace LiveSessionService.Application.Features.Playlists.Commands.CreateUserPlaylist;

public sealed record CreateUserPlaylistCommand(
    Guid UserId,
    string PlaylistName,
    bool IncludeInRequests,
    bool IncludeInOnDemand,
    bool IsEnabled,
    int PlaylistOrder,
    int Weight) : ICommand<UserPlaylistResult>;
