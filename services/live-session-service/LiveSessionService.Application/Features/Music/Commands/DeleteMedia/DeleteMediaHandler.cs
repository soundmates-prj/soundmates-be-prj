using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Exceptions;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Music.Commands.DeleteMedia;

public sealed class DeleteMediaHandler : ICommandHandler<DeleteMediaCommand>
{
    private const string SystemMediaPrefix = "system://";
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

        var isSystemMedia = !string.IsNullOrWhiteSpace(mediaFile.FilePath)
            && mediaFile.FilePath.StartsWith(SystemMediaPrefix, StringComparison.OrdinalIgnoreCase);

        if (isSystemMedia)
        {
            try
            {
                var relativePath = mediaFile.FilePath.Substring(SystemMediaPrefix.Length)
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);
                var absolutePath = Path.Combine(AppContext.BaseDirectory, "storage", relativePath);

                if (File.Exists(absolutePath))
                {
                    File.Delete(absolutePath);
                    _logger.LogInformation("Deleted local system media file at {Path}", absolutePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to delete local system media file {UniqueId}",
                    mediaFile.FilePath);
                return Result.Failure("Failed to delete local system media file", ErrorCode.InternalServerError);
            }
        }
        else
        {
            var stations = await _stationRepository.GetAllEnabledAsync(cancellationToken);
            if (stations.Count == 0)
                return Result.Failure("No enabled station found", ErrorCode.NotFound);

            var deletedInAzuraCast = false;
            foreach (var station in stations)
            {
                try
                {
                    await _azuraCastClient.DeleteMediaAsync(
                        station.ExternalStationId,
                        mediaFile.FilePath,
                        cancellationToken);

                    deletedInAzuraCast = true;
                    _logger.LogInformation(
                        "Deleted media file {MediaFileId} ({UniqueId}) from AzuraCast station {StationId}",
                        mediaFile.Id,
                        mediaFile.FilePath,
                        station.Id);
                    break;
                }
                catch (AzuraCastException ex) when (ex.ErrorCode == ErrorCode.NotFound)
                {
                    _logger.LogDebug(
                        "Media file {UniqueId} not found on station {StationId}, trying next station",
                        mediaFile.FilePath,
                        station.Id);
                }
                catch (AzuraCastException ex)
                {
                    _logger.LogError(ex,
                        "Failed to delete media file {UniqueId} from AzuraCast",
                        mediaFile.FilePath);
                    return Result.Failure(ex.Message, ex.ErrorCode);
                }
            }

            if (!deletedInAzuraCast)
            {
                _logger.LogWarning(
                    "Media file {UniqueId} was not found on any enabled AzuraCast station. Continuing local cleanup.",
                    mediaFile.FilePath);
            }
        }

        var playlistMedias = await _playlistMediaRepository.GetByMediaFileIdAsync(mediaFile.Id, cancellationToken);
        if (playlistMedias.Count > 0)
            await _playlistMediaRepository.DeleteRangeAsync(playlistMedias, cancellationToken);

        await _mediaFileRepository.DeleteAsync(mediaFile, cancellationToken);

        _logger.LogInformation(
            "Deleted media file {MediaFileId} ({UniqueId}) and removed {PlaylistMediaCount} playlist entries",
            mediaFile.Id,
            mediaFile.FilePath,
            playlistMedias.Count);

        return Result.Success();
    }
}
