using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Podcasts;

namespace LiveSessionService.Application.Features.Podcasts.Commands.FollowPodcast;

public sealed record FollowPodcastCommand(Guid UserId, Guid PodcastId) : ICommand<PodcastResult>;
