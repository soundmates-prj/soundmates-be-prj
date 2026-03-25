using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Playlists.Queries.GetUserPlaylists;

public sealed class GetUserPlaylistsHandler : IQueryHandler<GetUserPlaylistsQuery, List<UserPlaylistResult>>
{
    private readonly IUserPlaylistRepository _userPlaylistRepository;

    public GetUserPlaylistsHandler(IUserPlaylistRepository userPlaylistRepository)
    {
        _userPlaylistRepository = userPlaylistRepository;
    }

    public async Task<Result<List<UserPlaylistResult>>> Handle(GetUserPlaylistsQuery query, CancellationToken cancellationToken)
    {
        var playlists = await _userPlaylistRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        var results = playlists.Select(x => new UserPlaylistResult
        {
            Id = x.Id,
            UserId = x.UserId,
            PlaylistName = x.PlaylistName,
            IsEnabled = x.IsEnabled,
            IncludeInRequests = x.IncludeInRequests,
            IncludeInOnDemand = x.IncludeInOnDemand,
            PlaylistOrder = x.PlaylistOrder,
            Weight = x.Weight,
            TotalTracks = x.UserPlaylistMedias.Count,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        }).ToList();

        return Result<List<UserPlaylistResult>>.Success(results);
    }
}
