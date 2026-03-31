using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Podcasts;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Podcasts.Commands.CreatePodcastEpisode;

public sealed class CreatePodcastEpisodeHandler : ICommandHandler<CreatePodcastEpisodeCommand, PodcastEpisodeResult>
{
    private readonly IPodcastRepository _podcastRepository;

    public CreatePodcastEpisodeHandler(IPodcastRepository podcastRepository)
    {
        _podcastRepository = podcastRepository;
    }

    public async Task<Result<PodcastEpisodeResult>> Handle(CreatePodcastEpisodeCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Title) || string.IsNullOrWhiteSpace(command.AudioUrl))
        {
            return Result<PodcastEpisodeResult>.Failure("Title and AudioUrl are required", ErrorCode.BadRequest);
        }

        if (command.EpisodeNumber <= 0)
        {
            return Result<PodcastEpisodeResult>.Failure("EpisodeNumber must be greater than 0", ErrorCode.BadRequest);
        }

        if (command.Duration.HasValue && command.Duration.Value < 0)
        {
            return Result<PodcastEpisodeResult>.Failure("Duration cannot be negative", ErrorCode.BadRequest);
        }

        var podcast = await _podcastRepository.GetByIdAsync(command.PodcastId, cancellationToken);
        if (podcast == null)
        {
            return Result<PodcastEpisodeResult>.Failure("Podcast not found", ErrorCode.NotFound);
        }

        var duplicateEpisode = await _podcastRepository.GetEpisodeByNumberAsync(command.PodcastId, command.EpisodeNumber, cancellationToken);
        if (duplicateEpisode != null)
        {
            return Result<PodcastEpisodeResult>.Failure("Episode number already exists in this podcast", ErrorCode.Conflict);
        }

        var episode = new PodcastEpisode
        {
            Id = Guid.NewGuid(),
            PodcastId = command.PodcastId,
            Title = command.Title.Trim(),
            Description = command.Description,
            AudioUrl = command.AudioUrl.Trim(),
            ThumbnailUrl = command.ThumbnailUrl,
            EpisodeNumber = command.EpisodeNumber,
            PublishDate = command.PublishDate,
            Duration = command.Duration ?? 0
        };

        await _podcastRepository.AddEpisodeAsync(episode, cancellationToken);

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
