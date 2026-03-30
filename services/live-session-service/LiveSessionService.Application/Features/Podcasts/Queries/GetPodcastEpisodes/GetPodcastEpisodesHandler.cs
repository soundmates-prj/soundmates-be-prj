using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Podcasts;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Podcasts.Queries.GetPodcastEpisodes;

public sealed class GetPodcastEpisodesHandler : IQueryHandler<GetPodcastEpisodesQuery, List<PodcastEpisodeResult>>
{
    private readonly IPodcastRepository _podcastRepository;

    public GetPodcastEpisodesHandler(IPodcastRepository podcastRepository)
    {
        _podcastRepository = podcastRepository;
    }

    public async Task<Result<List<PodcastEpisodeResult>>> Handle(GetPodcastEpisodesQuery query, CancellationToken cancellationToken)
    {
        var podcast = await _podcastRepository.GetByIdAsync(query.PodcastId, cancellationToken);
        if (podcast == null)
        {
            return Result<List<PodcastEpisodeResult>>.Failure("Podcast not found", ErrorCode.NotFound);
        }

        var episodes = await _podcastRepository.GetEpisodesByPodcastIdAsync(query.PodcastId, cancellationToken);

        var result = episodes
            .Select(x => new PodcastEpisodeResult
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                AudioUrl = x.AudioUrl,
                ThumbnailUrl = x.ThumbnailUrl,
                EpisodeNumber = x.EpisodeNumber,
                PublishDate = x.PublishDate,
                Duration = x.Duration
            })
            .ToList();

        return Result<List<PodcastEpisodeResult>>.Success(result);
    }
}
