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
    private readonly IMediaFileRepository _mediaFileRepo;
    private readonly IDateTimeProvider _dateTime;
    private readonly ILogger<BulkUploadMusicHandler> _logger;
    private const string SystemMediaPrefix = "system://";

    private static readonly string[] AllowedExtensions = { ".mp3", ".flac", ".wav", ".ogg" };
    private const long MaxIndividualFileSize = 100 * 1024 * 1024; // 100MB
    private const long MaxTotalFileSize = 500 * 1024 * 1024; // 500MB

    public BulkUploadMusicHandler(
        IMediaFileRepository mediaFileRepo,
        IDateTimeProvider dateTime,
        ILogger<BulkUploadMusicHandler> logger)
    {
        _mediaFileRepo = mediaFileRepo;
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

        var uploadedFiles = new List<MusicResult>();
        var failedFiles = new List<BulkUploadFailedItem>();

        for (var index = 0; index < files.Count; index++)
        {
            var entry = files[index];

            try
            {
                var result = await ProcessSingleFileAsync(
                    entry, command.UploadedByUserId, cancellationToken);
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

        // Ensure stream is at position 0
        if (entry.FileStream.CanSeek)
            entry.FileStream.Position = 0;

        // Extract metadata from tags (same logic as single upload)
        string? tagTitle = null, tagArtist = null, tagAlbum = null;
        try
        {
            entry.FileStream.Position = 0;
            var abstraction = new TagLibStreamAbstraction(entry.FileStream, entry.FileName);
            using var tagFile = TagLib.File.Create(abstraction);
            tagTitle = string.IsNullOrWhiteSpace(tagFile.Tag.Title) ? null : tagFile.Tag.Title.Trim();
            tagArtist = tagFile.Tag.Performers?.Length > 0
                ? string.Join(", ", tagFile.Tag.Performers).Trim()
                : null;
            tagAlbum = string.IsNullOrWhiteSpace(tagFile.Tag.Album) ? null : tagFile.Tag.Album.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read tags from {FileName}", entry.FileName);
        }

        // Priority: request field > file tag > fallback
        entry.FileStream.Position = 0;
        var title = (!string.IsNullOrWhiteSpace(entry.Title) ? entry.Title : tagTitle)
                    ?? Path.GetFileNameWithoutExtension(entry.FileName);
        var artist = (!string.IsNullOrWhiteSpace(entry.Artist) ? entry.Artist : tagArtist)
                     ?? "Unknown Artist";
        var album = !string.IsNullOrWhiteSpace(entry.Album) ? entry.Album : tagAlbum;

        // Save file to storage
        var extension = extensionWithDot.TrimStart('.');
        var safeFileName = $"{Guid.NewGuid():N}{extensionWithDot}";
        var mediaDir = Path.Combine(AppContext.BaseDirectory, "storage", "system-media");
        Directory.CreateDirectory(mediaDir);

        var absolutePath = Path.Combine(mediaDir, safeFileName);
        var relativePath = $"system-media/{safeFileName}";

        entry.FileStream.Position = 0;
        await using (var fileStream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await entry.FileStream.CopyToAsync(fileStream, cancellationToken);
        }

        var mediaFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            Title = title,
            Artist = artist,
            Album = album,
            DurationSeconds = 0,
            FilePath = $"{SystemMediaPrefix}{relativePath}",
            FileType = extension,
            FileSizeBytes = entry.FileStream.Length,
            UploadedByUserId = uploadedByUserId,
            UploadedAt = _dateTime.UtcNow
        };

        await _mediaFileRepo.AddAsync(mediaFile, cancellationToken);

        _logger.LogInformation(
            "Bulk uploaded system media '{Title}' by '{Artist}' from '{FileName}'",
            mediaFile.Title, mediaFile.Artist, entry.FileName);

        return new MusicResult
        {
            Id = mediaFile.Id,
            SourceType = "system",
            Title = mediaFile.Title,
            Artist = mediaFile.Artist ?? string.Empty,
            Album = mediaFile.Album,
            Duration = mediaFile.DurationSeconds,
            FileUrl = relativePath,
            FileType = mediaFile.FileType,
            FileSize = mediaFile.FileSizeBytes,
            UploadedAt = mediaFile.UploadedAt
        };
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
