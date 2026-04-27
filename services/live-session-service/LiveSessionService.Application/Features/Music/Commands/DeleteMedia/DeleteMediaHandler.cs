using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Exceptions;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace LiveSessionService.Application.Features.Music.Commands.DeleteMedia;

public sealed class DeleteMediaHandler : ICommandHandler<DeleteMediaCommand>
{
    private const string SystemMediaPrefix = "system://";
    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly IPlaylistMediaRepository _playlistMediaRepository;
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly ICloudinaryMediaStorage _cloudinaryStorage;
    private readonly ILogger<DeleteMediaHandler> _logger;

    public DeleteMediaHandler(
        IMediaFileRepository mediaFileRepository,
        IPlaylistMediaRepository playlistMediaRepository,
        IAzuraCastStationRepository stationRepository,
        IAzuraCastClient azuraCastClient,
        ICloudinaryMediaStorage cloudinaryStorage,
        ILogger<DeleteMediaHandler> logger)
    {
        _mediaFileRepository = mediaFileRepository;
        _playlistMediaRepository = playlistMediaRepository;
        _stationRepository = stationRepository;
        _azuraCastClient = azuraCastClient;
        _cloudinaryStorage = cloudinaryStorage;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteMediaCommand command, CancellationToken cancellationToken)
    {
        var mediaFile = await _mediaFileRepository.GetByIdAsync(command.MediaFileId, cancellationToken);
        if (mediaFile == null)
            return Result.Failure("Media file not found", ErrorCode.NotFound);

        var localPath = mediaFile.FilePath;
        var azuraMediaId = mediaFile.AzuraCastMediaId;

        // If stored in Cloudinary (System Media)
        var isCloudinaryMedia = !string.IsNullOrWhiteSpace(localPath)
            && localPath.StartsWith("http", StringComparison.OrdinalIgnoreCase);

        if (isCloudinaryMedia)
        {
            try
            {
                var publicIdMatch = Regex.Match(localPath, @"\/v\d+\/(.+?)\.[a-zA-Z0-9]+$");
                if (publicIdMatch.Success)
                {
                    var publicId = publicIdMatch.Groups[1].Value;
                    await _cloudinaryStorage.DeleteAudioAsync(publicId, cancellationToken);
                    _logger.LogInformation("Deleted Cloudinary system media file with public_id {PublicId}", publicId);
                }
                
                if (!string.IsNullOrWhiteSpace(mediaFile.ArtUrl))
                {
                    var artPublicIdMatch = Regex.Match(mediaFile.ArtUrl, @"\/v\d+\/(.+?)\.[a-zA-Z0-9]+$");
                    if (artPublicIdMatch.Success)
                    {
                        var artPublicId = artPublicIdMatch.Groups[1].Value;
                        await _cloudinaryStorage.DeleteImageAsync(artPublicId, cancellationToken);
                        _logger.LogInformation("Deleted Cloudinary artwork with public_id {PublicId}", artPublicId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete Cloudinary media file {LocalPath}", localPath);
                // Continue to delete from DB even if Cloudinary fails to prevent orphaned DB records
            }
        }

        if (!string.IsNullOrWhiteSpace(azuraMediaId))
        {
            var stations = await _stationRepository.GetAllEnabledAsync(cancellationToken);
            if (stations.Count > 0)
            {
                var deletedInAzuraCast = false;
                foreach (var station in stations)
                {
                    try
                    {
                        await _azuraCastClient.DeleteMediaAsync(
                            station.ExternalStationId,
                            azuraMediaId,
                            cancellationToken);

                        deletedInAzuraCast = true;
                        _logger.LogInformation(
                            "Deleted media file {MediaFileId} ({AzuraCastMediaId}) from AzuraCast station {StationId}",
                            mediaFile.Id,
                            azuraMediaId,
                            station.Id);
                        break;
                    }
                    catch (AzuraCastException ex) when (ex.ErrorCode == ErrorCode.NotFound)
                    {
                        _logger.LogDebug(
                            "Media file {AzuraCastMediaId} not found on station {StationId}, trying next station",
                            azuraMediaId,
                            station.Id);
                    }
                    catch (AzuraCastException ex)
                    {
                        _logger.LogError(ex,
                            "Failed to delete media file {AzuraCastMediaId} from AzuraCast",
                            azuraMediaId);
                        return Result.Failure(ex.Message, ex.ErrorCode);
                    }
                }

                if (!deletedInAzuraCast)
                {
                    _logger.LogWarning(
                        "Media file {AzuraCastMediaId} was not found on any enabled AzuraCast station. Continuing local cleanup.",
                        azuraMediaId);
                }
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
