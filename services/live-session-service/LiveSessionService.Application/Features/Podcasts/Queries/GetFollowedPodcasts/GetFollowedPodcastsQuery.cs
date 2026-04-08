using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Podcasts;

namespace LiveSessionService.Application.Features.Podcasts.Queries.GetFollowedPodcasts;

public sealed record GetFollowedPodcastsQuery(Guid UserId) : IQuery<List<PodcastResult>>;
