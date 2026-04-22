using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.PodcastRequests;

namespace LiveSessionService.Application.Features.PodcastRequests.Queries.GetPodcastRequests;

public sealed record GetPodcastRequestsQuery(
    string? Status = null,
    string? SearchQuery = null) : IQuery<List<PodcastRequestResult>>;