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

        if (!command.IsAdmin && podcast.CreatedBy != command.UserId)
            return Result<PodcastResult>.Failure("You do not have permission to update this podcast", ErrorCode.Forbidden);

        var hasChanges = false;

        if (!string.IsNullOrWhiteSpace(command.Status))
        {
            if (!Enum.TryParse<PodcastStatus>(command.Status, true, out var status))
                return Result<PodcastResult>.Failure("Invalid podcast status", ErrorCode.BadRequest);

            if (podcast.Status != status)
            {
                podcast.Status = status;
                hasChanges = true;
            }
        }

        if (command.Title is not null && !string.Equals(podcast.Title, command.Title, StringComparison.Ordinal))
        {
            podcast.Title = command.Title;
            hasChanges = true;
        }

        if (command.Description is not null && !string.Equals(podcast.Description, command.Description, StringComparison.Ordinal))
        {
            podcast.Description = command.Description;
            hasChanges = true;
        }

        if (command.Author is not null && !string.Equals(podcast.Author, command.Author, StringComparison.Ordinal))
        {
            podcast.Author = command.Author;
            hasChanges = true;
        }

        if (command.Type is not null && !string.Equals(podcast.Type, command.Type, StringComparison.Ordinal))
        {
            podcast.Type = command.Type;
            hasChanges = true;
        }

        if (command.Banner is not null && !string.Equals(podcast.Banner, command.Banner, StringComparison.Ordinal))
        {
            podcast.Banner = command.Banner;
            hasChanges = true;
        }

        if (command.Price.HasValue && podcast.Price != command.Price.Value)
        {
            podcast.Price = command.Price.Value;
            hasChanges = true;
        }

        if (command.IsPaid.HasValue && podcast.IsPaid != command.IsPaid.Value)
        {
            podcast.IsPaid = command.IsPaid.Value;
            hasChanges = true;
        }

        if (hasChanges)
        {
            podcast.UpdatedAt = DateTime.SpecifyKind(_dateTimeProvider.UtcNow, DateTimeKind.Unspecified);
            await _podcastRepository.UpdateAsync(podcast, cancellationToken);
        }

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
