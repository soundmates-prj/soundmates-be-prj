using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Podcasts;

namespace LiveSessionService.Application.Features.Podcasts.Queries.GetPodcastEpisodeById;

public sealed record GetPodcastEpisodeByIdQuery(Guid PodcastId, Guid EpisodeId) : IQuery<PodcastEpisodeResult>;
