using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Podcasts;

namespace LiveSessionService.Application.Features.Podcasts.Queries.GetPodcast;

public sealed record GetPodcastQuery(Guid PodcastId) : IQuery<PodcastResult>;
