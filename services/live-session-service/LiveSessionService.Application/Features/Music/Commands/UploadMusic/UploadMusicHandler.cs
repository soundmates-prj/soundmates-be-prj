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
    private readonly IAzuraCastStationRepository _stationRepo;
    private readonly IMediaFileRepository        _mediaFileRepo;
    private readonly IAzuraCastClient            _azuraCast;
    private readonly IDateTimeProvider           _dateTime;
    private readonly ILogger<UploadMusicHandler> _logger;

    public UploadMusicHandler(
        IAzuraCastStationRepository stationRepo,
        IMediaFileRepository        mediaFileRepo,
        IAzuraCastClient            azuraCast,
        IDateTimeProvider           dateTime,
        ILogger<UploadMusicHandler> logger)
    {
        _stationRepo   = stationRepo;
        _mediaFileRepo = mediaFileRepo;
        _azuraCast     = azuraCast;
        _dateTime      = dateTime;
        _logger        = logger;
    }

    public async Task<Result<MusicResult>> Handle(
        UploadMusicCommand command,
        CancellationToken cancellationToken)
    {
        var station = await _stationRepo.GetByIdAsync(command.StationId, cancellationToken);
        if (station == null)
            return Result<MusicResult>.Failure("Station not found", ErrorCode.NotFound);

        // 1. Upload file to AzuraCast
        var media = await _azuraCast.UploadMediaAsync(
            station.ExternalStationId,
            command.FileStream,
            command.FileName,
            command.Title,
            command.Artist,
            command.Album,
            cancellationToken);

        if (media == null)
            return Result<MusicResult>.Failure(
                "Failed to upload media to AzuraCast", ErrorCode.InternalServerError);

        // 2. Save metadata to local DB
        // FilePath stores the AzuraCast unique_id for future API calls (e.g., assign to playlist)
        var extension = Path.GetExtension(command.FileName).TrimStart('.').ToLowerInvariant();
        var mediaFile = new MediaFile
        {
            Id              = Guid.NewGuid(),
            Title           = media.Title,
            Artist          = media.Artist,
            Album           = media.Album,
            DurationSeconds = media.DurationSeconds,
            FilePath        = media.UniqueId,   // AzuraCast unique_id
            FileType        = extension,
            FileSizeBytes   = command.FileStream.Length,
            UploadedByUserId = command.UploadedByUserId,
            UploadedAt      = _dateTime.UtcNow
        };

        await _mediaFileRepo.AddAsync(mediaFile, cancellationToken);

        _logger.LogInformation(
            "Uploaded media '{Title}' by '{Artist}' to station {StationId}",
            media.Title, media.Artist, command.StationId);

        return Result<MusicResult>.Success(new MusicResult
        {
            Id          = mediaFile.Id,
            StationId   = station.Id,
            Title       = mediaFile.Title,
            Artist      = mediaFile.Artist ?? string.Empty,
            Album       = mediaFile.Album,
            Duration    = mediaFile.DurationSeconds,
            FileUrl     = media.Path,
            FileType    = mediaFile.FileType,
            FileSize    = mediaFile.FileSizeBytes,
            UploadedAt  = mediaFile.UploadedAt
        });
    }
}
