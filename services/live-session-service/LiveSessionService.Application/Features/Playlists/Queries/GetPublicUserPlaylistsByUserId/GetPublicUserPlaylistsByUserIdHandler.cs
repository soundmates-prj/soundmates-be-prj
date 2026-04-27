using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Playlists.Queries.GetPublicUserPlaylistsByUserId;

public sealed class GetPublicUserPlaylistsByUserIdHandler : IQueryHandler<GetPublicUserPlaylistsByUserIdQuery, List<UserPlaylistResult>>
{
    private readonly IUserPlaylistRepository _userPlaylistRepository;

    public GetPublicUserPlaylistsByUserIdHandler(IUserPlaylistRepository userPlaylistRepository)
    {
        _userPlaylistRepository = userPlaylistRepository;
    }

    public async Task<Result<List<UserPlaylistResult>>> Handle(GetPublicUserPlaylistsByUserIdQuery query, CancellationToken cancellationToken)
    {
        var playlists = await _userPlaylistRepository.GetPublicPlaylistsByUserIdAsync(query.UserId, cancellationToken);

        var results = playlists.Select(x => new UserPlaylistResult
        {
            Id = x.Id,
            UserId = x.UserId,
            PlaylistName = x.PlaylistName,
            Description = x.Description,
            ThumbnailUrl = x.ThumbnailUrl,
            Visibility = x.Visibility,
            IsEnabled = x.IsEnabled,
            TotalTracks = x.UserPlaylistMedias.Count,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        }).ToList();

        return Result<List<UserPlaylistResult>>.Success(results);
    }
}
