using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.PodcastRequests.Queries.GetPodcastRequests;

namespace LiveSessionService.Application.Features.PodcastRequests.Queries.GetMyPodcastRequests;

public sealed record GetMyPodcastRequestsQuery(
    Guid UserId,
    string? Status = null) : IQuery<List<Application.Features.Results.PodcastRequests.PodcastRequestResult>>;