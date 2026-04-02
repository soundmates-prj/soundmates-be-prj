using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Playlists.Queries.GetUserPlaylistById;

public sealed class GetUserPlaylistByIdHandler : IQueryHandler<GetUserPlaylistByIdQuery, UserPlaylistResult>
{
    private readonly IUserPlaylistRepository _userPlaylistRepository;

    public GetUserPlaylistByIdHandler(IUserPlaylistRepository userPlaylistRepository)
    {
        _userPlaylistRepository = userPlaylistRepository;
    }

    public async Task<Result<UserPlaylistResult>> Handle(GetUserPlaylistByIdQuery query, CancellationToken cancellationToken)
    {
        var playlist = await _userPlaylistRepository.GetByIdAsync(query.PlaylistId, cancellationToken);
        if (playlist == null)
        {
            return Result<UserPlaylistResult>.Failure("Playlist not found", ErrorCode.NotFound);
        }

        if (playlist.UserId != query.UserId)
        {
            return Result<UserPlaylistResult>.Failure("You do not have permission to access this playlist", ErrorCode.Forbidden);
        }

        return Result<UserPlaylistResult>.Success(new UserPlaylistResult
        {
            Id = playlist.Id,
            UserId = playlist.UserId,
            PlaylistName = playlist.PlaylistName,
            Description = playlist.Description,
            ThumbnailUrl = playlist.ThumbnailUrl,
            Visibility = playlist.Visibility,
            IsEnabled = playlist.IsEnabled,
            TotalTracks = playlist.UserPlaylistMedias.Count,
            CreatedAt = playlist.CreatedAt,
            UpdatedAt = playlist.UpdatedAt
        });
    }
}
