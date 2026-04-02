using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Application.Features.Playlists.Commands.CreateUserPlaylist;

public sealed record CreateUserPlaylistCommand(
    Guid UserId,
    string PlaylistName,
    string? Description,
    string? ThumbnailUrl,
    PlaylistVisibility Visibility,
    bool IsEnabled) : ICommand<UserPlaylistResult>;
