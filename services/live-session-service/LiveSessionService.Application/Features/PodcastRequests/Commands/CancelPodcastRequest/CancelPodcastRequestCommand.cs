using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.PodcastRequests.Commands.ReviewPodcastRequest;

namespace LiveSessionService.Application.Features.PodcastRequests.Commands.CancelPodcastRequest;

public sealed record CancelPodcastRequestCommand(
    Guid PodcastRequestId,
    Guid UserId) : ICommand<bool>;