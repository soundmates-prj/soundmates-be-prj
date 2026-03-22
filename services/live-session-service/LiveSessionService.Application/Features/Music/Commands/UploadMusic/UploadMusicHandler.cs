using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Music;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Music.Commands.UploadMusic;

public sealed class UploadMusicHandler
    : ICommandHandler<UploadMusicCommand, MusicResult>
{
    private readonly IMediaFileRepository _mediaFileRepo;
    private readonly IDateTimeProvider _dateTime;
    private readonly ILogger<UploadMusicHandler> _logger;
    private const string SystemMediaPrefix = "system://";

    public UploadMusicHandler(
        IMediaFileRepository mediaFileRepo,
        IDateTimeProvider dateTime,
        ILogger<UploadMusicHandler> logger)
    {
        _mediaFileRepo = mediaFileRepo;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task<Result<MusicResult>> Handle(
        UploadMusicCommand command,
        CancellationToken cancellationToken)
    {
        var extensionWithDot = Path.GetExtension(command.FileName).ToLowerInvariant();
        var extension = extensionWithDot.TrimStart('.');
        var safeFileName = $"{Guid.NewGuid():N}{extensionWithDot}";
        var mediaDir = Path.Combine(AppContext.BaseDirectory, "storage", "system-media");
        Directory.CreateDirectory(mediaDir);

        var absolutePath = Path.Combine(mediaDir, safeFileName);
        var relativePath = $"system-media/{safeFileName}";

        if (command.FileStream.CanSeek)
        {
            command.FileStream.Position = 0;
        }

        await using (var fileStream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await command.FileStream.CopyToAsync(fileStream, cancellationToken);
        }

        var mediaFile = new MediaFile
        {
            Id               = Guid.NewGuid(),
            Title            = command.Title,
            Artist           = command.Artist,
            Album            = command.Album,
            DurationSeconds  = 0,
            FilePath         = $"{SystemMediaPrefix}{relativePath}",
            FileType         = extension,
            FileSizeBytes    = command.FileStream.Length,
            UploadedByUserId = command.UploadedByUserId,
            UploadedAt       = _dateTime.UtcNow
        };

        await _mediaFileRepo.AddAsync(mediaFile, cancellationToken);

        _logger.LogInformation(
            "Uploaded system media '{Title}' by '{Artist}' (separate from station media)",
            mediaFile.Title, mediaFile.Artist);

        return Result<MusicResult>.Success(new MusicResult
        {
            Id          = mediaFile.Id,
            SourceType  = "system",
            Title       = mediaFile.Title,
            Artist      = mediaFile.Artist ?? string.Empty,
            Album       = mediaFile.Album,
            Duration    = mediaFile.DurationSeconds,
            FileUrl     = relativePath,
            FileType    = mediaFile.FileType,
            FileSize    = mediaFile.FileSizeBytes,
            UploadedAt  = mediaFile.UploadedAt
        });
    }
}
