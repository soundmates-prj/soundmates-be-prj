using LiveSessionService.Application.Abstractions.Messaging;

namespace LiveSessionService.Application.Features.Playlists.Commands.DeleteUserPlaylist;

public sealed record DeleteUserPlaylistCommand(Guid PlaylistId, Guid UserId) : ICommand;
