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
    private const string SystemMediaPrefix = "system://cloudinary/";
    private readonly IStationPlaylistRepository _playlistRepo;
    private readonly IMediaFileRepository       _mediaFileRepo;
    private readonly IStationMediaFileRepository _stationMediaFileRepo;
    private readonly IAzuraCastClient            _azuraCast;
    private readonly ICloudinaryMediaStorage    _cloudinary;
    private readonly IDateTimeProvider          _dateTime;
    private readonly ILogger<AddMediaToPlaylistHandler> _logger;

    public AddMediaToPlaylistHandler(
        IStationPlaylistRepository   playlistRepo,
        IMediaFileRepository         mediaFileRepo,
        IStationMediaFileRepository  stationMediaFileRepo,
        IAzuraCastClient             azuraCast,
        ICloudinaryMediaStorage      cloudinary,
        IDateTimeProvider            dateTime,
        ILogger<AddMediaToPlaylistHandler> logger)
    {
        _playlistRepo  = playlistRepo;
        _mediaFileRepo = mediaFileRepo;
        _stationMediaFileRepo = stationMediaFileRepo;
        _azuraCast     = azuraCast;
        _cloudinary    = cloudinary;
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

        // Prefer station-specific mapping created by ImportSystemMediaBatch.
        // This avoids downloading/re-uploading an already imported system track.
        var stationMediaFile = await _stationMediaFileRepo.GetByMediaFileAndStationAsync(
            mediaFile.Id,
            playlist.AzuraCastStationId,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(stationMediaFile?.AzuraCastMediaId))
        {
            azuraMediaId = stationMediaFile.AzuraCastMediaId;
            _logger.LogInformation(
                "Resolved station media mapping for MediaId={MediaId}, StationId={StationId}, AzuraCastMediaId={AzuraCastMediaId}",
                mediaFile.Id,
                playlist.AzuraCastStationId,
                azuraMediaId);
        }

        // 3. If system media (stored on Cloudinary) and not yet on AzuraCast, download and re-upload
        if (string.IsNullOrWhiteSpace(azuraMediaId)
            && !string.IsNullOrWhiteSpace(mediaFile.FilePath)
            && mediaFile.FilePath.StartsWith(SystemMediaPrefix, StringComparison.OrdinalIgnoreCase))
        {
            // Extract Cloudinary public ID from "system://cloudinary/{publicId}"
            var publicId = mediaFile.FilePath.Substring(SystemMediaPrefix.Length).Trim();

            if (string.IsNullOrWhiteSpace(publicId))
            {
                _logger.LogWarning(
                    "Invalid system media FilePath — cannot extract Cloudinary publicId. MediaId={MediaId}, FilePath={FilePath}",
                    mediaFile.Id, mediaFile.FilePath);

                return Result<PlaylistMediaResult>.Failure(
                    $"File nhạc '{mediaFile.Title}' không hợp lệ hoặc chưa được upload đúng cách.",
                    ErrorCode.BadRequest);
            }

            _logger.LogInformation(
                "System media '{Title}' found on Cloudinary. Downloading and re-uploading to AzuraCast. PublicId={PublicId}",
                mediaFile.Title, publicId);

            Stream audioStream;
            try
            {
                audioStream = await _cloudinary.DownloadAudioAsync(publicId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to download system media from Cloudinary. MediaId={MediaId}, PublicId={PublicId}",
                    mediaFile.Id, publicId);

                return Result<PlaylistMediaResult>.Failure(
                    $"Không thể tải file nhạc '{mediaFile.Title}' từ Cloudinary. Vui lòng thử lại.",
                    ErrorCode.InternalServerError);
            }

            var fileName = $"{publicId.Replace("/", "_").Replace(" ", "_")}.{mediaFile.FileType}";
            await using (audioStream)
            {
                var uploaded = await _azuraCast.UploadMediaAsync(
                    playlist.AzuraCastStation.ExternalStationId,
                    audioStream,
                    fileName,
                    GetContentType(mediaFile.FileType),
                    mediaFile.Title,
                    mediaFile.Artist ?? "Unknown Artist",
                    mediaFile.Album,
                    cancellationToken);

                if (uploaded == null || string.IsNullOrWhiteSpace(uploaded.UniqueId))
                {
                    return Result<PlaylistMediaResult>.Failure(
                        $"Không thể upload file nhạc '{mediaFile.Title}' lên AzuraCast. Vui lòng kiểm tra cấu hình station.",
                        ErrorCode.InternalServerError);
                }

                azuraMediaId = uploaded.UniqueId;
            }

            // Update MediaFile with the new AzuraCast media ID
            mediaFile.AzuraCastMediaId = azuraMediaId;
            await _mediaFileRepo.UpdateAsync(mediaFile, cancellationToken);

            _logger.LogInformation(
                "Successfully re-uploaded system media '{Title}' to AzuraCast. AzuraCastMediaId={AzuraCastMediaId}",
                mediaFile.Title, azuraMediaId);
        }

        if (string.IsNullOrWhiteSpace(azuraMediaId))
        {
            return Result<PlaylistMediaResult>.Failure(
                $"File nhạc '{mediaFile.Title}' chưa được đồng bộ lên AzuraCast. Vui lòng sync station trước.",
                ErrorCode.BadRequest);
        }

        // 4. Assign in AzuraCast
        await _azuraCast.AssignMediaToPlaylistAsync(
            playlist.AzuraCastStation.ExternalStationId,
            azuraMediaId,
            playlist.ExternalPlaylistId,
            cancellationToken);

        // 5. Save PlaylistMedia record
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
            "Added media '{Title}' to playlist '{PlaylistId}'",
            mediaFile.Title, playlist.Id);

        return Result<PlaylistMediaResult>.Success(new PlaylistMediaResult
        {
            Id              = playlistMedia.Id,
            PlaylistId      = playlist.Id,
            MediaFileId     = mediaFile.Id,
            Title           = playlistMedia.SongTitle,
            Artist          = playlistMedia.SongArtist,
            Album           = playlistMedia.SongAlbum,
            ArtworkUrl      = mediaFile.ArtUrl,
            FileUrl         = mediaFile.FileUrl ?? mediaFile.FilePath,
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
