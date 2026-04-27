using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.PodcastRequests;

namespace LiveSessionService.Application.Features.PodcastEpisodeRequests.Commands.ReviewPodcastEpisodeRequest;

public sealed record ReviewPodcastEpisodeRequestCommand(
    Guid RequestId,
    bool IsApproved,
    string? RejectReason,
    Guid ReviewedByUserId) : ICommand<PodcastEpisodeRequestResult>;
