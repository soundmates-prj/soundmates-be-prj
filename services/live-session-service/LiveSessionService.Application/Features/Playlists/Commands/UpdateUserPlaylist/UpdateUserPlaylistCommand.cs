using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Application.Features.Playlists.Commands.UpdateUserPlaylist;

public sealed record UpdateUserPlaylistCommand(
    Guid PlaylistId,
    Guid UserId,
    string? PlaylistName,
    string? Description,
    string? ThumbnailUrl,
    PlaylistVisibility? Visibility,
    bool? IsEnabled) : ICommand<UserPlaylistResult>;
