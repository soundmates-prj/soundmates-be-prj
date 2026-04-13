using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Exceptions;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Music;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using TagLib;

namespace LiveSessionService.Application.Features.Music.Commands.BulkUploadMusic;

public sealed class BulkUploadMusicHandler
    : ICommandHandler<BulkUploadMusicCommand, BulkUploadMusicResult>
{
    private readonly IAzuraCastStationRepository _stationRepo;
    private readonly IMediaFileRepository _mediaFileRepo;
    private readonly ICloudinaryMediaStorage _cloudinaryStorage;
    private readonly IAzuraCastClient _azuraCast;
    private readonly IDateTimeProvider _dateTime;
    private readonly ILogger<BulkUploadMusicHandler> _logger;

    private static readonly string[] AllowedExtensions = { ".mp3", ".flac", ".wav", ".ogg" };
    private const long MaxIndividualFileSize = 100 * 1024 * 1024; // 100MB
    private const long MaxTotalFileSize = 500 * 1024 * 1024; // 500MB

    public BulkUploadMusicHandler(
        IAzuraCastStationRepository stationRepo,
        IMediaFileRepository mediaFileRepo,
        ICloudinaryMediaStorage cloudinaryStorage,
        IAzuraCastClient azuraCast,
        IDateTimeProvider dateTime,
        ILogger<BulkUploadMusicHandler> logger)
    {
        _stationRepo = stationRepo;
        _mediaFileRepo = mediaFileRepo;
        _cloudinaryStorage = cloudinaryStorage;
        _azuraCast = azuraCast;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task<Result<BulkUploadMusicResult>> Handle(
        BulkUploadMusicCommand command,
        CancellationToken cancellationToken)
    {
        var files = command.Files;

        if (files == null || files.Count == 0)
        {
            throw new AzuraCastException(
                "No files provided for bulk upload.",
                ErrorCode.BadRequest);
        }

        // Pre-validate: check file count and total size
        if (files.Count > 100)
        {
            throw new AzuraCastException(
                "Maximum 100 files allowed per bulk upload request.",
                ErrorCode.BadRequest);
        }

        var totalSize = files.Sum(f => f.FileStream is MemoryStream ms ? ms.Length : 0);
        if (totalSize > MaxTotalFileSize)
        {
            throw new AzuraCastException(
                $"Total file size exceeds maximum allowed (500MB). Actual: {totalSize / (1024 * 1024)}MB",
                ErrorCode.BadRequest);
        }

        AzuraCastStation? station = null;
        if (command.StationId.HasValue)
        {
            station = await _stationRepo.GetByIdAsync(command.StationId.Value, cancellationToken);
            if (station == null)
            {
                throw new AzuraCastException("Station not found", ErrorCode.NotFound);
            }
        }

        var uploadedFiles = new List<MusicResult>();
        var failedFiles = new List<BulkUploadFailedItem>();

        for (var index = 0; index < files.Count; index++)
        {
            var entry = files[index];

            try
            {
                var result = await ProcessSingleFileAsync(
                    entry,
                    station,
                    command.UploadedByUserId,
                    cancellationToken);
                uploadedFiles.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to upload file '{FileName}' at index {Index}",
                    entry.FileName, index);

                failedFiles.Add(new BulkUploadFailedItem
                {
                    FileName = entry.FileName,
                    ErrorMessage = ex.Message,
                    FileIndex = index
                });
            }
        }

        var bulkResult = new BulkUploadMusicResult
        {
            TotalFiles = files.Count,
            SuccessCount = uploadedFiles.Count,
            FailedCount = failedFiles.Count,
            UploadedFiles = uploadedFiles,
            FailedFiles = failedFiles
        };

        _logger.LogInformation(
            "Bulk upload completed: {SuccessCount}/{TotalFiles} succeeded. Failed: {FailedCount}",
            bulkResult.SuccessCount, bulkResult.TotalFiles, bulkResult.FailedCount);

        return Result<BulkUploadMusicResult>.Success(bulkResult);
    }

    private async Task<MusicResult> ProcessSingleFileAsync(
        BulkUploadFileEntry entry,
        AzuraCastStation? station,
        Guid uploadedByUserId,
        CancellationToken cancellationToken)
    {
        // Validate extension
        var extensionWithDot = Path.GetExtension(entry.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extensionWithDot))
        {
            throw new AzuraCastException(
                $"Unsupported file type '{extensionWithDot}'. Allowed: {string.Join(", ", AllowedExtensions)}",
                ErrorCode.BadRequest);
        }

        // Validate file size
        if (entry.FileStream.Length > MaxIndividualFileSize)
        {
            throw new AzuraCastException(
                $"File '{entry.FileName}' exceeds maximum size of 100MB. Actual: {entry.FileStream.Length / (1024 * 1024)}MB",
                ErrorCode.BadRequest);
        }

        var fileSizeBytes = entry.FileStream.CanSeek ? entry.FileStream.Length : 0;

        var (tagTitle, tagArtist, tagAlbum, artworkBytes, artworkExtension, durationSeconds) =
            await ExtractLocalMetadataAsync(entry.FileStream, entry.FileName, cancellationToken);

        // Priority: request field > file tag > fallback
        var title = (!string.IsNullOrWhiteSpace(entry.Title) ? entry.Title : tagTitle)
                    ?? Path.GetFileNameWithoutExtension(entry.FileName);
        var artist = (!string.IsNullOrWhiteSpace(entry.Artist) ? entry.Artist : tagArtist)
                     ?? "Unknown Artist";
        var album = !string.IsNullOrWhiteSpace(entry.Album) ? entry.Album : tagAlbum;

        var extension = extensionWithDot.TrimStart('.');

        CloudinaryUploadResult? artworkUpload = null;
        CloudinaryUploadResult? audioUpload = null;

        try
        {
            if (artworkBytes is { Length: > 0 })
            {
                var artworkFileName = $"art-{Guid.NewGuid():N}{artworkExtension ?? ".jpg"}";
                artworkUpload = await _cloudinaryStorage.UploadImageAsync(artworkBytes, artworkFileName, cancellationToken);
            }

            if (entry.FileStream.CanSeek)
                entry.FileStream.Position = 0;

            MediaFile mediaFile;

            if (station != null)
            {
                // Upload to AzuraCast only
                var media = await _azuraCast.UploadMediaAsync(
                    station.ExternalStationId,
                    entry.FileStream,
                    entry.FileName,
                    entry.ContentType,
                    title,
                    artist,
                    album,
                    cancellationToken);

                if (media == null)
                    throw new Exception("Failed to upload media to AzuraCast");

                mediaFile = new MediaFile
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Artist = artist,
                    Album = album,
                    ArtUrl = artworkUpload?.Url,
                    DurationSeconds = durationSeconds,
                    FilePath = media.UniqueId, // Same as UniqueId
                    AzuraCastMediaId = media.UniqueId, // Store song_id if possible
                    OriginalSourceType = "station",
                    FileType = extension,
                    FileSizeBytes = fileSizeBytes,
                    UploadedByUserId = uploadedByUserId,
                    UploadedAt = _dateTime.UtcNow
                };
            }
            else
            {
                // Upload to System (Cloudinary) only
                var safeFileName = $"audio-{Guid.NewGuid():N}{extensionWithDot}";
                audioUpload = await _cloudinaryStorage.UploadAudioAsync(entry.FileStream, safeFileName, cancellationToken);

                // Store FilePath with system:// prefix so ImportSystemMediaBatchHandler can detect it.
                // Actual playback URL is read from audioUpload.Url separately.
                var systemFilePath = $"system://cloudinary/{audioUpload.PublicId}";

                mediaFile = new MediaFile
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Artist = artist,
                    Album = album,
                    ArtUrl = artworkUpload?.Url,
                    DurationSeconds = durationSeconds,
                    FilePath = systemFilePath,
                    FileUrl = audioUpload.Url,
                    AzuraCastMediaId = null,
                    OriginalSourceType = "system",
                    FileType = extension,
                    FileSizeBytes = fileSizeBytes,
                    UploadedByUserId = uploadedByUserId,
                    UploadedAt = _dateTime.UtcNow
                };
            }

            await _mediaFileRepo.AddAsync(mediaFile, cancellationToken);

            _logger.LogInformation(
                "Bulk uploaded media '{Title}' by '{Artist}' from '{FileName}' to {Target}",
                mediaFile.Title,
                mediaFile.Artist,
                entry.FileName,
                station != null ? $"station {station.Id}" : "system");

            return new MusicResult
            {
                Id = mediaFile.Id,
                SourceType = station != null ? "station" : "system",
                Title = mediaFile.Title,
                Artist = mediaFile.Artist ?? string.Empty,
                Album = mediaFile.Album,
                ArtworkUrl = mediaFile.ArtUrl,
                Duration = mediaFile.DurationSeconds,
                FileUrl = audioUpload?.Url ?? mediaFile.FilePath,
                FileType = mediaFile.FileType,
                FileSize = mediaFile.FileSizeBytes,
                UploadedAt = mediaFile.UploadedAt,
                AzuraCastMediaId = mediaFile.AzuraCastMediaId,
            };
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

    private async Task<(string? TagTitle, string? TagArtist, string? TagAlbum, byte[]? ArtworkBytes, string? ArtworkExtension, int DurationSeconds)> ExtractLocalMetadataAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken)
    {
        string? tagTitle = null, tagArtist = null, tagAlbum = null;
        byte[]? artworkBytes = null;
        string? artworkExtension = null;
        var durationSeconds = 0;

        try
        {
            if (fileStream.CanSeek)
                fileStream.Position = 0;

            var abstraction = new TagLibStreamAbstraction(fileStream, fileName);
            using var tagFile = TagLib.File.Create(abstraction);

            tagTitle = string.IsNullOrWhiteSpace(tagFile.Tag.Title) ? null : tagFile.Tag.Title.Trim();
            tagArtist = tagFile.Tag.Performers?.Length > 0
                ? string.Join(", ", tagFile.Tag.Performers).Trim()
                : null;
            tagAlbum = string.IsNullOrWhiteSpace(tagFile.Tag.Album) ? null : tagFile.Tag.Album.Trim();

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
            _logger.LogWarning(ex, "Could not read tags/metadata from {FileName}", fileName);
        }
        finally
        {
            if (fileStream.CanSeek)
                fileStream.Position = 0;
        }

        return (tagTitle, tagArtist, tagAlbum, artworkBytes, artworkExtension, durationSeconds);
    }
}

/// <summary>
/// TagLib# abstraction for reading audio tags from a Stream
/// </summary>
internal sealed class TagLibStreamAbstraction : TagLib.File.IFileAbstraction
{
    private readonly Stream _stream;
    private readonly string _fileName;

    public TagLibStreamAbstraction(Stream stream, string fileName)
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
        // Don't dispose - we manage the stream externally
    }
}
