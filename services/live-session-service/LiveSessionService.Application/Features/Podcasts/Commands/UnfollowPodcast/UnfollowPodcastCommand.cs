using LiveSessionService.Application.Abstractions.Messaging;

namespace LiveSessionService.Application.Features.Podcasts.Commands.UnfollowPodcast;

public sealed record UnfollowPodcastCommand(Guid UserId, Guid PodcastId) : ICommand;
