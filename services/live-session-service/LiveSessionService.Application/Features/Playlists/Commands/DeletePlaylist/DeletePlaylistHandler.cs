using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Playlists.Commands.DeletePlaylist;

public sealed class DeletePlaylistHandler : ICommandHandler<DeletePlaylistCommand>
{
    private readonly IStationPlaylistRepository _playlistRepository;
    private readonly IPlaylistMediaRepository _playlistMediaRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly ILogger<DeletePlaylistHandler> _logger;

    public DeletePlaylistHandler(
        IStationPlaylistRepository playlistRepository,
        IPlaylistMediaRepository playlistMediaRepository,
        IAzuraCastClient azuraCastClient,
        ILogger<DeletePlaylistHandler> logger)
    {
        _playlistRepository = playlistRepository;
        _playlistMediaRepository = playlistMediaRepository;
        _azuraCastClient = azuraCastClient;
        _logger = logger;
    }

    public async Task<Result> Handle(DeletePlaylistCommand command, CancellationToken cancellationToken)
    {
        var playlist = await _playlistRepository.GetByIdAsync(command.PlaylistId, cancellationToken);
        if (playlist == null)
            return Result.Failure("Playlist not found", ErrorCode.NotFound);

        await _azuraCastClient.DeletePlaylistAsync(
            playlist.AzuraCastStation.ExternalStationId,
            playlist.ExternalPlaylistId,
            cancellationToken);

        await _playlistMediaRepository.DeleteByPlaylistIdAsync(playlist.Id, cancellationToken);
        await _playlistRepository.DeleteAsync(playlist, cancellationToken);

        _logger.LogInformation(
            "Deleted playlist '{PlaylistName}' (Id: {PlaylistId}, ExternalId: {ExternalId})",
            playlist.PlaylistName,
            playlist.Id,
            playlist.ExternalPlaylistId);

        return Result.Success();
    }
}
