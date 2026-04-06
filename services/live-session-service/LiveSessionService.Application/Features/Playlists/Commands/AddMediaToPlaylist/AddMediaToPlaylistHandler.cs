using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Playlists.Commands.AddMediaToPlaylist;

public sealed class AddMediaToPlaylistHandler
    : ICommandHandler<AddMediaToPlaylistCommand, PlaylistMediaResult>
{
    private const string SystemMediaPrefix = "system://";
    private readonly IStationPlaylistRepository      _playlistRepo;
    private readonly IMediaFileRepository            _mediaFileRepo;
    private readonly IAzuraCastClient                _azuraCast;
    private readonly IDateTimeProvider               _dateTime;
    private readonly ILogger<AddMediaToPlaylistHandler> _logger;

    public AddMediaToPlaylistHandler(
        IStationPlaylistRepository      playlistRepo,
        IMediaFileRepository            mediaFileRepo,
        IAzuraCastClient                azuraCast,
        IDateTimeProvider               dateTime,
        ILogger<AddMediaToPlaylistHandler> logger)
    {
        _playlistRepo  = playlistRepo;
        _mediaFileRepo = mediaFileRepo;
        _azuraCast     = azuraCast;
        _dateTime      = dateTime;
        _logger        = logger;
    }

    public async Task<Result<PlaylistMediaResult>> Handle(
        AddMediaToPlaylistCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Load playlist (includes AzuraCastStation navigation)
        var playlist = await _playlistRepo.GetByIdAsync(command.PlaylistId, cancellationToken);
        if (playlist == null)
            return Result<PlaylistMediaResult>.Failure("Playlist not found", ErrorCode.NotFound);

        // 2. Load media file
        var mediaFile = await _mediaFileRepo.GetByIdAsync(command.MediaFileId, cancellationToken);
        if (mediaFile == null)
            return Result<PlaylistMediaResult>.Failure("Media file not found", ErrorCode.NotFound);

        var azuraMediaId = mediaFile.AzuraCastMediaId;
        var title = mediaFile.Title;
        var artist = mediaFile.Artist;
        var album = mediaFile.Album;
        var duration = mediaFile.DurationSeconds;

        var localPath = mediaFile.FilePath;
        if (string.IsNullOrWhiteSpace(azuraMediaId)
            && !string.IsNullOrWhiteSpace(localPath)
            && localPath.StartsWith(SystemMediaPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = localPath.Substring(SystemMediaPrefix.Length)
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            var absolutePath = Path.Combine(AppContext.BaseDirectory, "storage", relativePath);

            if (!File.Exists(absolutePath))
            {
                _logger.LogWarning(
                    "System media file not found on disk. MediaId={MediaId}, FilePath={FilePath}, ExpectedPath={AbsolutePath}",
                    mediaFile.Id, mediaFile.FilePath, absolutePath);

                return Result<PlaylistMediaResult>.Failure(
                    $"File nhạc '{mediaFile.Title}' chưa được upload lên server. Vui lòng upload file trước khi thêm vào playlist. (Path: {absolutePath})",
                    ErrorCode.NotFound);
            }

            await using var fileStream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileName = Path.GetFileName(absolutePath);
            var uploaded = await _azuraCast.UploadMediaAsync(
                playlist.AzuraCastStation.ExternalStationId,
                fileStream,
                fileName,
                GetContentType(mediaFile.FileType),
                mediaFile.Title,
                mediaFile.Artist ?? "Unknown Artist",
                mediaFile.Album,
                cancellationToken);

            if (uploaded == null || string.IsNullOrWhiteSpace(uploaded.UniqueId))
            {
                return Result<PlaylistMediaResult>.Failure(
                    "Failed to import system media into station",
                    ErrorCode.InternalServerError);
            }

            azuraMediaId = uploaded.UniqueId;
            title = mediaFile.Title;
            artist = mediaFile.Artist;
            album = mediaFile.Album;
            duration = mediaFile.DurationSeconds;

            mediaFile.AzuraCastMediaId = uploaded.UniqueId;
            await _mediaFileRepo.UpdateAsync(mediaFile, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(azuraMediaId))
        {
            return Result<PlaylistMediaResult>.Failure("Media has not been synchronized to AzuraCast", ErrorCode.BadRequest);
        }

        // 3. Assign in AzuraCast
        await _azuraCast.AssignMediaToPlaylistAsync(
            playlist.AzuraCastStation.ExternalStationId,
            azuraMediaId,
            playlist.ExternalPlaylistId,
            cancellationToken);

        // 4. Save PlaylistMedia record
        var playlistMedia = new PlaylistMedia
        {
            Id                = Guid.NewGuid(),
            StationPlaylistId = playlist.Id,
            MediaFileId       = mediaFile.Id,
            MediaId           = azuraMediaId,
            SongTitle         = title,
            SongArtist        = artist,
            SongAlbum         = album,
            DurationSeconds   = duration,
            FilePath          = azuraMediaId,
            IsEnabled         = true,
            Weight            = 1,
            CreatedAt         = _dateTime.UtcNow
        };

        await _playlistRepo.AddMediaAsync(playlistMedia, cancellationToken);

        _logger.LogInformation(
            "Added media '{Title}' to playlist '{Playlist}'",
            mediaFile.Title, playlist.PlaylistName);

        return Result<PlaylistMediaResult>.Success(new PlaylistMediaResult
        {
            Id              = playlistMedia.Id,
            PlaylistId      = playlist.Id,
            MediaFileId     = mediaFile.Id,
            Title           = playlistMedia.SongTitle,
            Artist          = playlistMedia.SongArtist,
            Album           = playlistMedia.SongAlbum,
            ArtworkUrl      = mediaFile.ArtUrl,
            FileUrl         = mediaFile.FilePath,
            FileType        = mediaFile.FileType,
            FileSize        = mediaFile.FileSizeBytes,
            DurationSeconds = playlistMedia.DurationSeconds,
            AddedAt         = playlistMedia.CreatedAt
        });
    }

    private static string GetContentType(string fileType)
    {
        return fileType.ToLowerInvariant() switch
        {
            "mp3" => "audio/mpeg",
            "flac" => "audio/flac",
            "wav" => "audio/wav",
            "ogg" => "audio/ogg",
            _ => "application/octet-stream"
        };
    }
}
