using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Playlists.Queries.GetAllPublicUserPlaylists;

public sealed class GetAllPublicUserPlaylistsHandler : IQueryHandler<GetAllPublicUserPlaylistsQuery, List<UserPlaylistResult>>
{
    private readonly IUserPlaylistRepository _userPlaylistRepository;

    public GetAllPublicUserPlaylistsHandler(IUserPlaylistRepository userPlaylistRepository)
    {
        _userPlaylistRepository = userPlaylistRepository;
    }

    public async Task<Result<List<UserPlaylistResult>>> Handle(GetAllPublicUserPlaylistsQuery query, CancellationToken cancellationToken)
    {
        var playlists = await _userPlaylistRepository.GetAllPublicAsync(cancellationToken);

        var results = playlists.Select(x => new UserPlaylistResult
        {
            Id = x.Id,
            UserId = x.UserId,
            PlaylistName = x.PlaylistName,
            Description = x.Description,
            ThumbnailUrl = x.ThumbnailUrl,
            Visibility = x.Visibility,
            IsEnabled = x.IsEnabled,
            TotalTracks = x.UserPlaylistMedias?.Count ?? 0,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        }).ToList();

        return Result<List<UserPlaylistResult>>.Success(results);
    }
}
