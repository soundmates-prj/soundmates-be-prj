using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Playlists;

namespace LiveSessionService.Application.Features.Playlists.Commands.AddMediaToPlaylist;

public sealed record AddMediaToPlaylistCommand(
    Guid PlaylistId,
    Guid MediaFileId) : ICommand<PlaylistMediaResult>;
