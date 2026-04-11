using LiveSessionService.Application.Abstractions.Messaging;

namespace LiveSessionService.Application.Features.PodcastRequests.Commands.ReviewPodcastRequest;

public sealed record ReviewPodcastRequestCommand(
    Guid PodcastRequestId,
    Guid ReviewedByUserId,
    bool IsApproved,
    string? RejectReason) : ICommand<Application.Features.Results.PodcastRequests.PodcastRequestResult>;