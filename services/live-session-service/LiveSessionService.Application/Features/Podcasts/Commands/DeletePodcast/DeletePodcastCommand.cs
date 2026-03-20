using LiveSessionService.Application.Abstractions.Messaging;

namespace LiveSessionService.Application.Features.Podcasts.Commands.DeletePodcast;

public sealed record DeletePodcastCommand(Guid PodcastId) : ICommand;
