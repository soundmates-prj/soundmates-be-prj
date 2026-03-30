using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Podcasts;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Podcasts.Queries.GetPodcastEpisodeById;

public sealed class GetPodcastEpisodeByIdHandler : IQueryHandler<GetPodcastEpisodeByIdQuery, PodcastEpisodeResult>
{
    private readonly IPodcastRepository _podcastRepository;

    public GetPodcastEpisodeByIdHandler(IPodcastRepository podcastRepository)
    {
        _podcastRepository = podcastRepository;
    }

    public async Task<Result<PodcastEpisodeResult>> Handle(GetPodcastEpisodeByIdQuery query, CancellationToken cancellationToken)
    {
        var podcast = await _podcastRepository.GetByIdAsync(query.PodcastId, cancellationToken);
        if (podcast == null)
        {
            return Result<PodcastEpisodeResult>.Failure("Podcast not found", ErrorCode.NotFound);
        }

        var episode = await _podcastRepository.GetEpisodeByIdAsync(query.EpisodeId, cancellationToken);
        if (episode == null || episode.PodcastId != query.PodcastId)
        {
            return Result<PodcastEpisodeResult>.Failure("Podcast episode not found", ErrorCode.NotFound);
        }

        return Result<PodcastEpisodeResult>.Success(new PodcastEpisodeResult
        {
            Id = episode.Id,
            Title = episode.Title,
            Description = episode.Description,
            AudioUrl = episode.AudioUrl,
            ThumbnailUrl = episode.ThumbnailUrl,
            EpisodeNumber = episode.EpisodeNumber,
            PublishDate = episode.PublishDate,
            Duration = episode.Duration
        });
    }
}
