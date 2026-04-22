using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.PodcastRequests;

namespace LiveSessionService.Application.Features.PodcastEpisodeRequests.Queries.GetPodcastEpisodeRequestById;

public sealed record GetPodcastEpisodeRequestByIdQuery(Guid Id) : IQuery<PodcastEpisodeRequestResult>;
