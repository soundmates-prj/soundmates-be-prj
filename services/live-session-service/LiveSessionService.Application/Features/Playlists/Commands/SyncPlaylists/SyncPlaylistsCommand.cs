using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Playlists;

namespace LiveSessionService.Application.Features.Playlists.Commands.SyncPlaylists;

/// <summary>
/// Command to sync playlists from AzuraCast for a specific station
/// </summary>
public sealed record SyncPlaylistsCommand(Guid StationId) : ICommand<SyncPlaylistsResult>;
