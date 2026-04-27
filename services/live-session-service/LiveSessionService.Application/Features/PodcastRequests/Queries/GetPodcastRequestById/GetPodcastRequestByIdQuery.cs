using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.PodcastRequests;

namespace LiveSessionService.Application.Features.PodcastRequests.Queries.GetPodcastRequestById;

public sealed record GetPodcastRequestByIdQuery(Guid Id) : IQuery<PodcastRequestResult>;