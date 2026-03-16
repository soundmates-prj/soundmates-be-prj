using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Music.Commands.DeleteMedia;

public sealed class DeleteMediaHandler : ICommandHandler<DeleteMediaCommand>
{
    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly IPlaylistMediaRepository _playlistMediaRepository;
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly ILogger<DeleteMediaHandler> _logger;

    public DeleteMediaHandler(
        IMediaFileRepository mediaFileRepository,
        IPlaylistMediaRepository playlistMediaRepository,
        IAzuraCastStationRepository stationRepository,
        IAzuraCastClient azuraCastClient,
        ILogger<DeleteMediaHandler> logger)
    {
        _mediaFileRepository = mediaFileRepository;
        _playlistMediaRepository = playlistMediaRepository;
        _stationRepository = stationRepository;
        _azuraCastClient = azuraCastClient;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteMediaCommand command, CancellationToken cancellationToken)
    {
        var mediaFile = await _mediaFileRepository.GetByIdAsync(command.MediaFileId, cancellationToken);
        if (mediaFile == null)
            return Result.Failure("Media file not found", ErrorCode.NotFound);

        var station = await _stationRepository.GetByIdAsync(mediaFile.StationId, cancellationToken);
        if (station == null)
            return Result.Failure("Station not found", ErrorCode.NotFound);

        await _azuraCastClient.DeleteMediaAsync(
            station.ExternalStationId,
            mediaFile.FilePath,
            cancellationToken);

        var playlistMedias = await _playlistMediaRepository.GetByMediaFileIdAsync(mediaFile.Id, cancellationToken);
        if (playlistMedias.Count > 0)
            await _playlistMediaRepository.DeleteRangeAsync(playlistMedias, cancellationToken);

        await _mediaFileRepository.DeleteAsync(mediaFile, cancellationToken);

        _logger.LogInformation(
            "Deleted media file {MediaFileId} ({UniqueId}) from station {StationId} and removed {PlaylistMediaCount} playlist entries",
            mediaFile.Id,
            mediaFile.FilePath,
            station.Id,
            playlistMedias.Count);

        return Result.Success();
    }
}
