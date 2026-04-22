using LiveSessionService.Application.Abstractions.Messaging;

namespace LiveSessionService.Application.Features.PodcastRequests.Commands.CreatePodcastRequest;

public sealed record CreatePodcastRequestCommand(
    Guid RequestedByUserId,
    string? AuthorInfo,
    string Title,
    string EpisodeTitle,
    string? Description,
    string? BannerUrl,
    string AudioUrl,
    decimal Price,
    bool IsPaid) : ICommand<Application.Features.Results.PodcastRequests.PodcastRequestResult>;