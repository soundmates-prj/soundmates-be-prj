using LiveSessionService.Application.Abstractions.Messaging;

namespace LiveSessionService.Application.Features.Playlists.Commands.DeletePlaylist;

public sealed record DeletePlaylistCommand(Guid PlaylistId) : ICommand;
