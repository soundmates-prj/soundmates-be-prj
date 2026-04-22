using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.PodcastRequests;

namespace LiveSessionService.Application.Features.PodcastEpisodeRequests.Commands.CreatePodcastEpisodeRequest;

public sealed record CreatePodcastEpisodeRequestCommand(
    Guid PodcastId,
    Guid RequestedByUserId,
    string? AuthorInfo,
    string Title,
    string? Description,
    string? ThumbnailUrl,
    string AudioUrl,
    int Duration) : ICommand<PodcastEpisodeRequestResult>;
