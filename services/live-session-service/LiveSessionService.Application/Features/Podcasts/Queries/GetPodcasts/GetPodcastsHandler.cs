using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Podcasts;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Podcasts.Queries.GetPodcasts;

public sealed class GetPodcastsHandler : IQueryHandler<GetPodcastsQuery, List<PodcastResult>>
{
    private readonly IPodcastRepository _podcastRepository;

    public GetPodcastsHandler(IPodcastRepository podcastRepository)
    {
        _podcastRepository = podcastRepository;
    }

    public async Task<Result<List<PodcastResult>>> Handle(GetPodcastsQuery query, CancellationToken cancellationToken)
    {
        PodcastStatus? status = null;

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<PodcastStatus>(query.Status, true, out var parsedStatus))
                return Result<List<PodcastResult>>.Failure("Invalid podcast status", ErrorCode.BadRequest);

            status = parsedStatus;
        }

        var podcasts = await _podcastRepository.GetAllAsync(query.CreatedBy, status, cancellationToken);

        var result = podcasts.Select(x =>
        {
            object? parsedAuthor = null;
            if (!string.IsNullOrWhiteSpace(x.Author))
            {
                var a = x.Author.Trim();
                if (a.Length > 0 && (a[0] == '{' || a[0] == '[' || a[0] == '"'))
                {
                    try
                    {
                        parsedAuthor = System.Text.Json.JsonSerializer.Deserialize<object>(a);
                    }
                    catch (Exception)
                    {
                        parsedAuthor = a;
                    }
                }
                else
                {
                    parsedAuthor = a;
                }
            }

            return new PodcastResult
            {
                Id = x.Id,
                Price = x.Price,
                IsPaid = x.IsPaid,
                Title = x.Title,
                Description = x.Description,
                Author = parsedAuthor,
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
            };
        }).ToList();

        return Result<List<PodcastResult>>.Success(result);
    }
}
