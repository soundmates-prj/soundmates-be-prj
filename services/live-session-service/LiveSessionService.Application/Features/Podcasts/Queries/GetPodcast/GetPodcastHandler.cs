using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Podcasts;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Podcasts.Queries.GetPodcast;

public sealed class GetPodcastHandler : IQueryHandler<GetPodcastQuery, PodcastResult>
{
    private readonly IPodcastRepository _podcastRepository;

    public GetPodcastHandler(IPodcastRepository podcastRepository)
    {
        _podcastRepository = podcastRepository;
    }

    public async Task<Result<PodcastResult>> Handle(GetPodcastQuery query, CancellationToken cancellationToken)
    {
        var podcast = await _podcastRepository.GetByIdAsync(query.PodcastId, cancellationToken);
        if (podcast == null)
            return Result<PodcastResult>.Failure("Podcast not found", ErrorCode.NotFound);

        object? parsedAuthor = null;
        if (!string.IsNullOrWhiteSpace(podcast.Author))
        {
            var a = podcast.Author.Trim();
            if (a.Length > 0 && (a[0] == '{' || a[0] == '[' || a[0] == '"'))
            {
                try { parsedAuthor = System.Text.Json.JsonSerializer.Deserialize<object>(a); }
                catch (Exception) { parsedAuthor = a; }
            }
            else { parsedAuthor = a; }
        }

        return Result<PodcastResult>.Success(new PodcastResult
        {
            Id = podcast.Id,
            Price = podcast.Price,
            IsPaid = podcast.IsPaid,
            Title = podcast.Title,
            Description = podcast.Description,
            Author = parsedAuthor,
            Status = podcast.Status.ToString(),
            Type = podcast.Type,
            Banner = podcast.Banner,
            CreatedAt = podcast.CreatedAt,
            UpdatedAt = podcast.UpdatedAt,
            CreatedBy = podcast.CreatedBy,
            EpisodeCount = podcast.Episodes.Count,
            AllEpisodes = podcast.Episodes
                .OrderBy(x => x.EpisodeNumber)
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
                .ToList()
        });
    }
}
