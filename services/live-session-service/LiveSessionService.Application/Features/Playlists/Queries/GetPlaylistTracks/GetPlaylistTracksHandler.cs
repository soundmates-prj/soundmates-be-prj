using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Playlists.Queries.GetPlaylistTracks;

public sealed class GetPlaylistTracksHandler : IQueryHandler<GetPlaylistTracksQuery, List<PlaylistMediaResult>>
{
    private readonly IStationPlaylistRepository _playlistRepository;
    private readonly IPlaylistMediaRepository _playlistMediaRepository;
    private readonly ILogger<GetPlaylistTracksHandler> _logger;

    public GetPlaylistTracksHandler(
        IStationPlaylistRepository playlistRepository,
        IPlaylistMediaRepository playlistMediaRepository,
        ILogger<GetPlaylistTracksHandler> logger)
    {
        _playlistRepository = playlistRepository;
        _playlistMediaRepository = playlistMediaRepository;
        _logger = logger;
    }

    public async Task<Result<List<PlaylistMediaResult>>> Handle(
        GetPlaylistTracksQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var playlist = await _playlistRepository.GetByIdAsync(query.PlaylistId, cancellationToken);
            if (playlist == null)
                return Result<List<PlaylistMediaResult>>.Failure("Playlist not found", ErrorCode.NotFound);

            var playlistTracks = await _playlistMediaRepository.GetByPlaylistIdAsync(query.PlaylistId, cancellationToken);

            var result = playlistTracks.Select(track => new PlaylistMediaResult
            {
                Id = track.Id,
                PlaylistId = track.StationPlaylistId,
                MediaFileId = track.MediaFileId ?? Guid.Empty,
                Title = track.SongTitle,
                Artist = track.SongArtist,
                Album = track.SongAlbum,
                ArtworkUrl = track.MediaFile?.ArtUrl ?? track.SongArtUrl,
                FileUrl = track.MediaFile?.FilePath ?? track.FilePath,
                FileType = track.MediaFile?.FileType,
                FileSize = track.MediaFile?.FileSizeBytes,
                DurationSeconds = track.DurationSeconds,
                AddedAt = track.CreatedAt
            }).ToList();

            return Result<List<PlaylistMediaResult>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tracks for playlist {PlaylistId}", query.PlaylistId);
            return Result<List<PlaylistMediaResult>>.Failure(
                "Failed to retrieve playlist tracks",
                ErrorCode.InternalServerError);
        }
    }
}
