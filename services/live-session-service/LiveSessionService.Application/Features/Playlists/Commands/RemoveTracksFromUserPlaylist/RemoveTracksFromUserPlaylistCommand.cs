using LiveSessionService.Application.Abstractions.Messaging;

namespace LiveSessionService.Application.Features.Playlists.Commands.RemoveTracksFromUserPlaylist;

public sealed record RemoveTracksFromUserPlaylistCommand(
    Guid PlaylistId,
    Guid UserId,
    List<Guid> MediaIds) : ICommand;
