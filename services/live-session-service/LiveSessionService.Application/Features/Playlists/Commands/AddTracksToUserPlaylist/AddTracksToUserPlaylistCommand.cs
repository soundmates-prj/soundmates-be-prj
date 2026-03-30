using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Playlists;

namespace LiveSessionService.Application.Features.Playlists.Commands.AddTracksToUserPlaylist;

public sealed record AddTracksToUserPlaylistCommand(
    Guid PlaylistId,
    Guid UserId,
    List<Guid> MediaIds) : ICommand<List<PlaylistMediaResult>>;
