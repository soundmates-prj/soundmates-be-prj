using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Podcasts;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Podcasts.Commands.UpdatePodcast;

public sealed class UpdatePodcastHandler : ICommandHandler<UpdatePodcastCommand, PodcastResult>
{
    private readonly IPodcastRepository _podcastRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdatePodcastHandler(IPodcastRepository podcastRepository, IDateTimeProvider dateTimeProvider)
    {
        _podcastRepository = podcastRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PodcastResult>> Handle(UpdatePodcastCommand command, CancellationToken cancellationToken)
    {
        var podcast = await _podcastRepository.GetByIdAsync(command.PodcastId, cancellationToken);
        if (podcast == null)
            return Result<PodcastResult>.Failure("Podcast not found", ErrorCode.NotFound);

        if (!string.IsNullOrWhiteSpace(command.Status))
        {
            if (!Enum.TryParse<PodcastStatus>(command.Status, true, out var status))
                return Result<PodcastResult>.Failure("Invalid podcast status", ErrorCode.BadRequest);

            podcast.Status = status;
        }

        podcast.Title = command.Title;
        podcast.Description = command.Description;
        podcast.Author = command.Author;
        podcast.Type = command.Type;
        podcast.Banner = command.Banner;
        podcast.UpdatedAt = _dateTimeProvider.UtcNow;

        await _podcastRepository.UpdateAsync(podcast, cancellationToken);

        return Result<PodcastResult>.Success(new PodcastResult
        {
            Id = podcast.Id,
            Title = podcast.Title,
            Description = podcast.Description,
            Author = podcast.Author,
            Status = podcast.Status.ToString(),
            Type = podcast.Type,
            Banner = podcast.Banner,
            CreatedAt = podcast.CreatedAt,
            UpdatedAt = podcast.UpdatedAt,
            CreatedBy = podcast.CreatedBy,
            EpisodeCount = podcast.Episodes.Count
        });
    }
}
