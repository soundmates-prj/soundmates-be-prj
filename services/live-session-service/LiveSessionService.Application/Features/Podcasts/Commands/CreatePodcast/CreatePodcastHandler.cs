using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Podcasts;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Podcasts.Commands.CreatePodcast;

public sealed class CreatePodcastHandler : ICommandHandler<CreatePodcastCommand, PodcastResult>
{
    private readonly IPodcastRepository _podcastRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreatePodcastHandler(IPodcastRepository podcastRepository, IDateTimeProvider dateTimeProvider)
    {
        _podcastRepository = podcastRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PodcastResult>> Handle(CreatePodcastCommand command, CancellationToken cancellationToken)
    {
        var podcast = new Podcast
        {
            Id = Guid.NewGuid(),
            CreatedBy = command.CreatedBy,
            Title = command.Title,
            Description = command.Description,
            Author = command.Author,
            Type = command.Type,
            Banner = command.Banner,
            Status = command.Status,
            Price = command.Price,
            IsPaid = command.IsPaid,
            CreatedAt = _dateTimeProvider.UtcNow
        };

        await _podcastRepository.AddAsync(podcast, cancellationToken);

        object? parsedAuthor = null;
        if (!string.IsNullOrWhiteSpace(podcast.Author))
        {
            var a = podcast.Author.Trim();
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
            EpisodeCount = 0,
            AllEpisodes = new List<PodcastEpisodeResult>()
        });
    }
}
