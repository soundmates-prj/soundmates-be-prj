using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Podcasts;

namespace LiveSessionService.Application.Features.Podcasts.Commands.CreatePodcastEpisode;

public sealed record CreatePodcastEpisodeCommand(
    Guid PodcastId,
    string Title,
    string? Description,
    string AudioUrl,
    string? ThumbnailUrl,
    int EpisodeNumber,
    DateTime PublishDate,
    int? Duration) : ICommand<PodcastEpisodeResult>;
