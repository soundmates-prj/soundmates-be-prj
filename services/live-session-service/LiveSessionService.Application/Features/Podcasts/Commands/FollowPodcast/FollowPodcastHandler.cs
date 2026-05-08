using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Podcasts;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Podcasts.Commands.FollowPodcast;

public sealed class FollowPodcastHandler : ICommandHandler<FollowPodcastCommand, PodcastResult>
{
    private readonly IPodcastRepository _podcastRepository;
    private readonly IUserSavedPodcastRepository _userSavedPodcastRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public FollowPodcastHandler(
        IPodcastRepository podcastRepository,
        IUserSavedPodcastRepository userSavedPodcastRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _podcastRepository = podcastRepository;
        _userSavedPodcastRepository = userSavedPodcastRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PodcastResult>> Handle(FollowPodcastCommand command, CancellationToken cancellationToken)
    {
        var podcast = await _podcastRepository.GetByIdAsync(command.PodcastId, cancellationToken);
        if (podcast == null)
            return Result<PodcastResult>.Failure("Podcast not found", ErrorCode.NotFound);

        var existing = await _userSavedPodcastRepository.GetByUserAndPodcastAsync(command.UserId, command.PodcastId, cancellationToken);
        if (existing != null)
            return Result<PodcastResult>.Failure("You already follow this podcast", ErrorCode.Conflict);

        var userSavedPodcast = new UserSavedPodcast
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            PodcastId = command.PodcastId,
            SavedAt = _dateTimeProvider.UtcNow
        };

        await _userSavedPodcastRepository.AddAsync(userSavedPodcast, cancellationToken);

        return Result<PodcastResult>.Success(new PodcastResult
        {
            Id = podcast.Id,
            Price = podcast.Price,
            IsPaid = podcast.IsPaid,
            Title = podcast.Title,
            Description = podcast.Description,
            Author = string.IsNullOrWhiteSpace(podcast.Author) ? null : 
                     (podcast.Author.TrimStart().StartsWith("{") || podcast.Author.TrimStart().StartsWith("[")) 
                        ? System.Text.Json.JsonSerializer.Deserialize<object>(podcast.Author) 
                        : podcast.Author,
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
