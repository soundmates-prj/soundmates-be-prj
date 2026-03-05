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

        // 3. Assign in AzuraCast (FilePath stores the AzuraCast unique_id)
        await _azuraCast.AssignMediaToPlaylistAsync(
            playlist.AzuraCastStation.ExternalStationId,
            mediaFile.FilePath,                    // AzuraCast unique_id
            playlist.ExternalPlaylistId,
            cancellationToken);

        // 4. Save PlaylistMedia record
        var playlistMedia = new PlaylistMedia
        {
            Id                = Guid.NewGuid(),
            StationPlaylistId = playlist.Id,
            MediaFileId       = mediaFile.Id,
            MediaId           = mediaFile.FilePath,  // AzuraCast unique_id
            SongTitle         = mediaFile.Title,
            SongArtist        = mediaFile.Artist,
            SongAlbum         = mediaFile.Album,
            DurationSeconds   = mediaFile.DurationSeconds,
            FilePath          = mediaFile.FilePath,
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
            Title           = mediaFile.Title,
            Artist          = mediaFile.Artist,
            Album           = mediaFile.Album,
            DurationSeconds = mediaFile.DurationSeconds,
            AddedAt         = playlistMedia.CreatedAt
        });
    }
}
