using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Podcasts;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Podcasts.Queries.GetFollowedPodcasts;

public sealed class GetFollowedPodcastsHandler : IQueryHandler<GetFollowedPodcastsQuery, List<PodcastResult>>
{
    private readonly IUserSavedPodcastRepository _userSavedPodcastRepository;

    public GetFollowedPodcastsHandler(IUserSavedPodcastRepository userSavedPodcastRepository)
    {
        _userSavedPodcastRepository = userSavedPodcastRepository;
    }

    public async Task<Result<List<PodcastResult>>> Handle(GetFollowedPodcastsQuery query, CancellationToken cancellationToken)
    {
        var followedPodcasts = await _userSavedPodcastRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        var results = followedPodcasts
            .Select(x => x.Podcast)
            .Select(x => new PodcastResult
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                Author = x.Author,
                Status = x.Status.ToString(),
                Type = x.Type,
                Banner = x.Banner,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                CreatedBy = x.CreatedBy,
                EpisodeCount = x.Episodes.Count,
                AllEpisodes = x.Episodes
                    .OrderBy(e => e.EpisodeNumber)
                    .Select(e => new PodcastEpisodeResult
                    {
                        Id = e.Id,
                        Title = e.Title,
                        Description = e.Description,
                        AudioUrl = e.AudioUrl,
                        ThumbnailUrl = e.ThumbnailUrl,
                        EpisodeNumber = e.EpisodeNumber,
                        PublishDate = e.PublishDate,
                        Duration = e.Duration
                    })
                    .ToList()
            })
            .ToList();

        return Result<List<PodcastResult>>.Success(results);
    }
}
