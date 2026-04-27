using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Podcasts;

namespace LiveSessionService.Application.Features.Podcasts.Commands.UpdatePodcastEpisode;

public sealed record UpdatePodcastEpisodeCommand(
    Guid PodcastId,
    Guid EpisodeId,
    string Title,
    string? Description,
    string? AudioUrl,
    string? ThumbnailUrl,
    int EpisodeNumber,
    DateTime PublishDate,
    int? Duration) : ICommand<PodcastEpisodeResult>;
