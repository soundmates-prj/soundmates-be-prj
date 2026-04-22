using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Podcasts;

namespace LiveSessionService.Application.Features.Podcasts.Queries.GetMyPodcasts;

public sealed record GetMyPodcastsQuery(Guid UserId, string? Status) : IQuery<List<PodcastResult>>;
