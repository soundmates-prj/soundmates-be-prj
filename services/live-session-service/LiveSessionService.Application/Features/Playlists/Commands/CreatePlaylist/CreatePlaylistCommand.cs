using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Playlists;

namespace LiveSessionService.Application.Features.Playlists.Commands.CreatePlaylist;

/// <summary>
/// Command to create a playlist for a station
/// </summary>
public sealed record CreatePlaylistCommand(
    Guid StationId,
    string PlaylistName,
    string? Description,
    bool IsAutoPlay) : ICommand<PlaylistResult>;
