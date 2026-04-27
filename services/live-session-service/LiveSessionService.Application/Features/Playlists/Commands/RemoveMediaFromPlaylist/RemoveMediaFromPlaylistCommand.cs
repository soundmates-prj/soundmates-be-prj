using LiveSessionService.Application.Abstractions.Messaging;

namespace LiveSessionService.Application.Features.Playlists.Commands.RemoveMediaFromPlaylist;

public sealed record RemoveMediaFromPlaylistCommand(
    Guid PlaylistId,
    Guid MediaFileId) : ICommand;
