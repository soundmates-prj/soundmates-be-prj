using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Music;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using TagLib;

namespace LiveSessionService.Application.Features.Music.Commands.UploadMusic;

public sealed class UploadMusicHandler
    : ICommandHandler<UploadMusicCommand, MusicResult>
{
    private readonly IAzuraCastStationRepository _stationRepo;
    private readonly IMediaFileRepository _mediaFileRepo;
    private readonly IAzuraCastClient _azuraCast;
    private readonly ICloudinaryMediaStorage _cloudinaryStorage;
    private readonly IDateTimeProvider _dateTime;
    private readonly ILogger<UploadMusicHandler> _logger;

    public UploadMusicHandler(
        IAzuraCastStationRepository stationRepo,
        IMediaFileRepository mediaFileRepo,
        IAzuraCastClient azuraCast,
        ICloudinaryMediaStorage cloudinaryStorage,
        IDateTimeProvider dateTime,
        ILogger<UploadMusicHandler> logger)
    {
        _stationRepo = stationRepo;
        _mediaFileRepo = mediaFileRepo;
        _azuraCast = azuraCast;
        _cloudinaryStorage = cloudinaryStorage;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task<Result<MusicResult>> Handle(
        UploadMusicCommand command,
        CancellationToken cancellationToken)
    {
        AzuraCastStation? station = null;
        if (command.StationId.HasValue)
        {
            station = await _stationRepo.GetByIdAsync(command.StationId.Value, cancellationToken);
            if (station == null)
                return Result<MusicResult>.Failure("Station not found", ErrorCode.NotFound);
        }

        var extensionWithDot = Path.GetExtension(command.FileName).ToLowerInvariant();
        var extension = extensionWithDot.TrimStart('.');
        var fileSizeBytes = command.FileStream.CanSeek ? command.FileStream.Length : 0;

        var (artworkBytes, artworkExtension, localDurationSeconds) = await ExtractLocalMetadataAsync(
            command.FileStream,
            command.FileName,
            cancellationToken);

        CloudinaryUploadResult? artworkUpload = null;
        CloudinaryUploadResult? audioUpload = null;

        try
        {
            if (artworkBytes is { Length: > 0 })
            {
                var artworkFileName = $"art-{Guid.NewGuid():N}{artworkExtension ?? ".jpg"}";
                artworkUpload = await _cloudinaryStorage.UploadImageAsync(artworkBytes, artworkFileName, cancellationToken);
            }

            if (command.FileStream.CanSeek)
                command.FileStream.Position = 0;

            MediaFile mediaFile;

            if (station != null)
            {
                // Upload to AzuraCast only
                var media = await _azuraCast.UploadMediaAsync(
                    station.ExternalStationId,
                    command.FileStream,
                    command.FileName,
                    command.ContentType,
                    command.Title,
                    command.Artist,
                    command.Album,
                    cancellationToken);

                if (media == null)
                    return Result<MusicResult>.Failure("Failed to upload media to AzuraCast", ErrorCode.InternalServerError);

                try
                {
                    await _azuraCast.UpdateMediaMetadataAsync(
                        station.ExternalStationId,
                        media.UniqueId,
                        command.Title,
                        command.Artist,
                        command.Album,
                        command.Lyrics,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    // Keep upload successful even if metadata sync fails.
                    _logger.LogWarning(
                        ex,
                        "Uploaded media {UniqueId} to station {StationId} but failed to sync metadata (lyrics/title/artist/album) to AzuraCast.",
                        media.UniqueId,
                        station.ExternalStationId);
                }

                mediaFile = new MediaFile
                {
                    Id = Guid.NewGuid(),
                    Title = command.Title,
                    Artist = command.Artist,
                    Album = command.Album,
                    ArtUrl = artworkUpload?.Url,
                    Lyrics = command.Lyrics,
                    DurationSeconds = localDurationSeconds,
                    FilePath = media.UniqueId, // Same as UniqueId for station media
                    FileUrl = null,
                    AzuraCastMediaId = media.UniqueId,
                    OriginalSourceType = "station",
                    FileType = extension,
                    FileSizeBytes = fileSizeBytes,
                    UploadedByUserId = command.UploadedByUserId,
                    UploadedAt = _dateTime.UtcNow
                };
            }
            else
            {
                // Upload to System (Cloudinary) only
                var cloudAudioFileName = $"audio-{Guid.NewGuid():N}{extensionWithDot}";
                audioUpload = await _cloudinaryStorage.UploadAudioAsync(command.FileStream, cloudAudioFileName, cancellationToken);

                var systemFilePath = $"system://cloudinary/{audioUpload.PublicId}";

                mediaFile = new MediaFile
                {
                    Id = Guid.NewGuid(),
                    Title = command.Title,
                    Artist = command.Artist,
                    Album = command.Album,
                    ArtUrl = artworkUpload?.Url,
                    Lyrics = command.Lyrics,
                    DurationSeconds = localDurationSeconds,
                    FilePath = systemFilePath,
                    FileUrl = audioUpload.Url,
                    AzuraCastMediaId = null,
                    OriginalSourceType = "system",
                    FileType = extension,
                    FileSizeBytes = fileSizeBytes,
                    UploadedByUserId = command.UploadedByUserId,
                    UploadedAt = _dateTime.UtcNow
                };
            }

            await _mediaFileRepo.AddAsync(mediaFile, cancellationToken);

            _logger.LogInformation(
                "Uploaded media '{Title}' by '{Artist}' to {Target}. CloudinaryPath={CloudinaryPath}, AzuraCastId={AzuraCastId}",
                mediaFile.Title,
                mediaFile.Artist,
                station != null ? $"station {station.Id}" : "system",
                audioUpload?.Url,
                mediaFile.AzuraCastMediaId);

            return Result<MusicResult>.Success(new MusicResult
            {
                Id = mediaFile.Id,
                SourceType = station != null ? "station" : "system",
                Title = mediaFile.Title,
                Artist = mediaFile.Artist ?? string.Empty,
                Album = mediaFile.Album,
                ArtworkUrl = mediaFile.ArtUrl,
                Lyrics = mediaFile.Lyrics,
                Duration = mediaFile.DurationSeconds,
                FileUrl = mediaFile.FileUrl ?? mediaFile.FilePath,
                FileType = mediaFile.FileType,
                FileSize = mediaFile.FileSizeBytes,
                UploadedAt = mediaFile.UploadedAt
            });
        }
        catch
        {
            if (audioUpload != null)
                await _cloudinaryStorage.DeleteAudioAsync(audioUpload.PublicId, cancellationToken);

            if (artworkUpload != null)
                await _cloudinaryStorage.DeleteImageAsync(artworkUpload.PublicId, cancellationToken);

            throw;
        }
    }

    private async Task<(byte[]? ArtworkBytes, string? ArtworkExtension, int DurationSeconds)> ExtractLocalMetadataAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken)
    {
        byte[]? artworkBytes = null;
        string? artworkExtension = null;
        var durationSeconds = 0;

        try
        {
            if (fileStream.CanSeek)
                fileStream.Position = 0;

            var abstraction = new UploadTagLibStreamAbstraction(fileStream, fileName);
            using var tagFile = TagLib.File.Create(abstraction);

            durationSeconds = (int)Math.Max(0, tagFile.Properties.Duration.TotalSeconds);

            var picture = tagFile.Tag.Pictures?.FirstOrDefault();
            if (picture?.Data != null && picture.Data.Count > 0)
            {
                artworkBytes = picture.Data.Data;
                artworkExtension = picture.MimeType?.ToLowerInvariant() switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/gif" => ".gif",
                    "image/webp" => ".webp",
                    _ => ".jpg"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract local metadata from file {FileName}", fileName);
        }
        finally
        {
            if (fileStream.CanSeek)
                fileStream.Position = 0;
        }

        return (artworkBytes, artworkExtension, durationSeconds);
    }
}

internal sealed class UploadTagLibStreamAbstraction : TagLib.File.IFileAbstraction
{
    private readonly Stream _stream;
    private readonly string _fileName;

    public UploadTagLibStreamAbstraction(Stream stream, string fileName)
    {
        _stream = stream;
        _fileName = fileName;
    }

    public string Name => _fileName;

    public Stream ReadStream => _stream;

    public Stream WriteStream
    {
        get
        {
            _stream.Position = 0;
            return _stream;
        }
    }

    public void CloseStream(Stream stream)
    {
        // managed externally
    }
}
