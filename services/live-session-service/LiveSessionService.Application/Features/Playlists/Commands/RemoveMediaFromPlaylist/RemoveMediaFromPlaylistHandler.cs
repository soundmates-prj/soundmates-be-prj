using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Playlists.Commands.RemoveMediaFromPlaylist;

public sealed class RemoveMediaFromPlaylistHandler : ICommandHandler<RemoveMediaFromPlaylistCommand>
{
    private readonly IStationPlaylistRepository _playlistRepo;
    private readonly IMediaFileRepository _mediaFileRepo;
    private readonly IPlaylistMediaRepository _playlistMediaRepo;
    private readonly IAzuraCastClient _azuraCast;
    private readonly ILogger<RemoveMediaFromPlaylistHandler> _logger;

    public RemoveMediaFromPlaylistHandler(
        IStationPlaylistRepository playlistRepo,
        IMediaFileRepository mediaFileRepo,
        IPlaylistMediaRepository playlistMediaRepo,
        IAzuraCastClient azuraCast,
        ILogger<RemoveMediaFromPlaylistHandler> logger)
    {
        _playlistRepo = playlistRepo;
        _mediaFileRepo = mediaFileRepo;
        _playlistMediaRepo = playlistMediaRepo;
        _azuraCast = azuraCast;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveMediaFromPlaylistCommand command, CancellationToken cancellationToken)
    {
        var playlist = await _playlistRepo.GetByIdAsync(command.PlaylistId, cancellationToken);
        if (playlist == null)
            return Result.Failure("Playlist not found", ErrorCode.NotFound);

        var mediaFile = await _mediaFileRepo.GetByIdAsync(command.MediaFileId, cancellationToken);
        if (mediaFile == null)
            return Result.Failure("Media file not found", ErrorCode.NotFound);

        var playlistMedia = await _playlistMediaRepo.GetByPlaylistAndMediaIdAsync(
            command.PlaylistId,
            mediaFile.FilePath,
            cancellationToken);

        if (playlistMedia == null)
            return Result.Failure("Track is not in this playlist", ErrorCode.NotFound);

        await _azuraCast.RemoveMediaFromPlaylistAsync(
            playlist.AzuraCastStation.ExternalStationId,
            mediaFile.FilePath,
            playlist.ExternalPlaylistId,
            cancellationToken);

        await _playlistMediaRepo.DeleteAsync(playlistMedia, cancellationToken);

        _logger.LogInformation(
            "Removed media '{Title}' from playlist '{Playlist}'",
            mediaFile.Title,
            playlist.PlaylistName);

        return Result.Success();
    }
}
