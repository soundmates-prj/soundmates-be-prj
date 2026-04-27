using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Podcasts;

namespace LiveSessionService.Application.Features.Podcasts.Queries.GetPodcastEpisodes;

public sealed record GetPodcastEpisodesQuery(Guid PodcastId) : IQuery<List<PodcastEpisodeResult>>;
