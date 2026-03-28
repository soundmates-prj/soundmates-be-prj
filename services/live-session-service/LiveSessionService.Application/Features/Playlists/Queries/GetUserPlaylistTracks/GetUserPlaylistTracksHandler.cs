using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Playlists.Queries.GetUserPlaylistTracks;

public sealed class GetUserPlaylistTracksHandler : IQueryHandler<GetUserPlaylistTracksQuery, List<PlaylistMediaResult>>
{
    private readonly IUserPlaylistRepository _userPlaylistRepository;

    public GetUserPlaylistTracksHandler(IUserPlaylistRepository userPlaylistRepository)
    {
        _userPlaylistRepository = userPlaylistRepository;
    }

    public async Task<Result<List<PlaylistMediaResult>>> Handle(GetUserPlaylistTracksQuery query, CancellationToken cancellationToken)
    {
        var playlist = await _userPlaylistRepository.GetByIdAsync(query.PlaylistId, cancellationToken);
        if (playlist == null)
        {
            return Result<List<PlaylistMediaResult>>.Failure("Playlist not found", ErrorCode.NotFound);
        }

        if (playlist.UserId != query.UserId)
        {
            return Result<List<PlaylistMediaResult>>.Failure("You do not have permission to access this playlist", ErrorCode.Forbidden);
        }

        var tracks = await _userPlaylistRepository.GetTracksAsync(query.PlaylistId, cancellationToken);

        var result = tracks
            .Where(x => x.MediaFile != null)
            .Select(x => new PlaylistMediaResult
            {
                Id = x.Id,
                PlaylistId = x.UserPlaylistId,
                MediaFileId = x.MediaFileId!.Value,
                Title = x.MediaFile!.Title,
                Artist = x.MediaFile.Artist,
                Album = x.MediaFile.Album,
                DurationSeconds = x.MediaFile.DurationSeconds,
                AddedAt = x.CreatedAt
            })
            .ToList();

        return Result<List<PlaylistMediaResult>>.Success(result);
    }
}
