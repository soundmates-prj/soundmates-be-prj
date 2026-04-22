using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.PodcastRequests;

namespace LiveSessionService.Application.Features.PodcastEpisodeRequests.Queries.GetMyPodcastEpisodeRequests;

public sealed record GetMyPodcastEpisodeRequestsQuery(Guid UserId, string? Status) : IQuery<List<PodcastEpisodeRequestResult>>;
