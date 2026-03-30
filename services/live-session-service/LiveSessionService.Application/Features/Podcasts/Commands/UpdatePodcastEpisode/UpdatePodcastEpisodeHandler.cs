using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Podcasts;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Podcasts.Commands.UpdatePodcastEpisode;

public sealed class UpdatePodcastEpisodeHandler : ICommandHandler<UpdatePodcastEpisodeCommand, PodcastEpisodeResult>
{
    private readonly IPodcastRepository _podcastRepository;

    public UpdatePodcastEpisodeHandler(IPodcastRepository podcastRepository)
    {
        _podcastRepository = podcastRepository;
    }

    public async Task<Result<PodcastEpisodeResult>> Handle(UpdatePodcastEpisodeCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return Result<PodcastEpisodeResult>.Failure("Title is required", ErrorCode.BadRequest);
        }

        if (command.AudioUrl is not null && string.IsNullOrWhiteSpace(command.AudioUrl))
        {
            return Result<PodcastEpisodeResult>.Failure("AudioUrl cannot be empty", ErrorCode.BadRequest);
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

        var episode = await _podcastRepository.GetEpisodeByIdAsync(command.EpisodeId, cancellationToken);
        if (episode == null || episode.PodcastId != command.PodcastId)
        {
            return Result<PodcastEpisodeResult>.Failure("Podcast episode not found", ErrorCode.NotFound);
        }

        if (episode.EpisodeNumber != command.EpisodeNumber)
        {
            var duplicateEpisode = await _podcastRepository.GetEpisodeByNumberAsync(command.PodcastId, command.EpisodeNumber, cancellationToken);
            if (duplicateEpisode != null && duplicateEpisode.Id != episode.Id)
            {
                return Result<PodcastEpisodeResult>.Failure("Episode number already exists in this podcast", ErrorCode.Conflict);
            }
        }

        episode.Title = command.Title.Trim();
        episode.Description = command.Description;

        if (command.AudioUrl is not null)
        {
            episode.AudioUrl = command.AudioUrl.Trim();
        }

        if (command.ThumbnailUrl is not null)
        {
            episode.ThumbnailUrl = string.IsNullOrWhiteSpace(command.ThumbnailUrl)
                ? null
                : command.ThumbnailUrl.Trim();
        }

        episode.EpisodeNumber = command.EpisodeNumber;
        episode.PublishDate = command.PublishDate;
        episode.Duration = command.Duration ?? episode.Duration;

        await _podcastRepository.UpdateEpisodeAsync(episode, cancellationToken);

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
