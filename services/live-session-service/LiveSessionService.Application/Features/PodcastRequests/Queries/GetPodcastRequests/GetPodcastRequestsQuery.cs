using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.PodcastRequests;

namespace LiveSessionService.Application.Features.PodcastRequests.Queries.GetPodcastRequests;

public sealed record GetPodcastRequestsQuery(
    Guid? LiveSessionId = null,
    string? Status = null,
    string? SearchQuery = null) : IQuery<List<PodcastRequestResult>>;