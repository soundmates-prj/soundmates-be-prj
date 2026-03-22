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

        var azuraMediaId = mediaFile.FilePath;
        var title = mediaFile.Title;
        var artist = mediaFile.Artist;
        var album = mediaFile.Album;
        var duration = mediaFile.DurationSeconds;

        if (!string.IsNullOrWhiteSpace(mediaFile.FilePath)
            && mediaFile.FilePath.StartsWith(SystemMediaPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = mediaFile.FilePath.Substring(SystemMediaPrefix.Length)
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            var absolutePath = Path.Combine(AppContext.BaseDirectory, "storage", relativePath);

            if (!File.Exists(absolutePath))
            {
                return Result<PlaylistMediaResult>.Failure(
                    "System media file not found on server storage",
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
            title = uploaded.Title;
            artist = uploaded.Artist;
            album = uploaded.Album;
            duration = uploaded.DurationSeconds;
        }

        // 3. Assign in AzuraCast (FilePath stores the AzuraCast unique_id)
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
