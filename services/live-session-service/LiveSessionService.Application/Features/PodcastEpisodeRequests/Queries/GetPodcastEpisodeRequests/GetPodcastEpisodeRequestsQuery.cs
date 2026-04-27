using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.PodcastRequests;

namespace LiveSessionService.Application.Features.PodcastEpisodeRequests.Queries.GetPodcastEpisodeRequests;

public sealed record GetPodcastEpisodeRequestsQuery(string? Status, string? Search) : IQuery<List<PodcastEpisodeRequestResult>>;
