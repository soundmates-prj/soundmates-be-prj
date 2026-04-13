using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Music;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Music.Commands.UpdateMusicMetadata;

public sealed class UpdateMusicMetadataHandler
    : ICommandHandler<UpdateMusicMetadataCommand, MusicResult>
{
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly IStationMediaFileRepository _stationMediaFileRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<UpdateMusicMetadataHandler> _logger;

    public UpdateMusicMetadataHandler(
        IAzuraCastStationRepository stationRepository,
        IMediaFileRepository mediaFileRepository,
        IStationMediaFileRepository stationMediaFileRepository,
        IAzuraCastClient azuraCastClient,
        IDateTimeProvider dateTimeProvider,
        ILogger<UpdateMusicMetadataHandler> logger)
    {
        _stationRepository = stationRepository;
        _mediaFileRepository = mediaFileRepository;
        _stationMediaFileRepository = stationMediaFileRepository;
        _azuraCastClient = azuraCastClient;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<MusicResult>> Handle(
        UpdateMusicMetadataCommand command,
        CancellationToken cancellationToken)
    {
        var station = await _stationRepository.GetByIdAsync(command.StationId, cancellationToken);
        if (station == null)
        {
            return Result<MusicResult>.Failure("Station not found", ErrorCode.NotFound);
        }

        var mediaFile = await _mediaFileRepository.GetByIdAsync(command.MusicId, cancellationToken);
        if (mediaFile == null)
        {
            return Result<MusicResult>.Failure("Media file not found", ErrorCode.NotFound);
        }

        var stationMapping = await _stationMediaFileRepository.GetByMediaFileAndStationAsync(
            mediaFile.Id,
            station.Id,
            cancellationToken);

        if (string.Equals(mediaFile.OriginalSourceType, "system", StringComparison.OrdinalIgnoreCase)
            && stationMapping == null)
        {
            return Result<MusicResult>.Failure(
                "Media file has not been imported to this station",
                ErrorCode.BadRequest);
        }

        var azuraCastMediaId = !string.IsNullOrWhiteSpace(stationMapping?.AzuraCastMediaId)
            ? stationMapping!.AzuraCastMediaId
            : mediaFile.AzuraCastMediaId;

        if (string.IsNullOrWhiteSpace(azuraCastMediaId))
        {
            return Result<MusicResult>.Failure(
                "Media file is not available on AzuraCast for this station",
                ErrorCode.BadRequest);
        }

        var title = string.IsNullOrWhiteSpace(command.Title)
            ? mediaFile.Title
            : command.Title.Trim();

        var artist = command.Artist is null
            ? mediaFile.Artist
            : string.IsNullOrWhiteSpace(command.Artist)
                ? null
                : command.Artist.Trim();

        var album = command.Album is null
            ? mediaFile.Album
            : string.IsNullOrWhiteSpace(command.Album)
                ? null
                : command.Album.Trim();

        var lyrics = command.Lyrics is null
            ? mediaFile.Lyrics
            : string.IsNullOrWhiteSpace(command.Lyrics)
                ? null
                : command.Lyrics.Trim();

        try
        {
            await _azuraCastClient.UpdateMediaMetadataAsync(
                station.ExternalStationId,
                azuraCastMediaId,
                title,
                artist,
                album,
                lyrics,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update metadata for MediaId={MediaId} (AzuraCastMediaId={AzuraCastMediaId}) on StationId={StationId}",
                mediaFile.Id,
                azuraCastMediaId,
                station.Id);

            return Result<MusicResult>.Failure(
                "Failed to sync metadata to AzuraCast",
                ErrorCode.InternalServerError);
        }

        mediaFile.Title = title;
        mediaFile.Artist = artist;
        mediaFile.Album = album;
        mediaFile.Lyrics = lyrics;
        mediaFile.UpdatedAt = _dateTimeProvider.UtcNow;

        await _mediaFileRepository.UpdateAsync(mediaFile, cancellationToken);

        return Result<MusicResult>.Success(new MusicResult
        {
            Id = mediaFile.Id,
            SourceType = "station",
            Title = mediaFile.Title,
            Artist = mediaFile.Artist ?? string.Empty,
            Album = mediaFile.Album,
            ArtworkUrl = mediaFile.ArtUrl,
            Lyrics = mediaFile.Lyrics,
            Duration = mediaFile.DurationSeconds,
            FileUrl = !string.IsNullOrWhiteSpace(mediaFile.FileUrl)
                ? mediaFile.FileUrl
                : mediaFile.FilePath,
            FileType = mediaFile.FileType,
            FileSize = mediaFile.FileSizeBytes,
            UploadedAt = mediaFile.UploadedAt,
            AzuraCastMediaId = azuraCastMediaId,
        });
    }
}
