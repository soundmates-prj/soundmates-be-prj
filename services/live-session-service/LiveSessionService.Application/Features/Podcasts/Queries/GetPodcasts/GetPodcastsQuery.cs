using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Podcasts;

namespace LiveSessionService.Application.Features.Podcasts.Queries.GetPodcasts;

public sealed record GetPodcastsQuery(Guid? CreatedBy, string? Status, Guid? UserId = null) : IQuery<List<PodcastResult>>;
