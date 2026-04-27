using LiveSessionService.Application.Abstractions.Messaging;

namespace LiveSessionService.Application.Features.Podcasts.Commands.DeletePodcastEpisode;

public sealed record DeletePodcastEpisodeCommand(Guid PodcastId, Guid EpisodeId) : ICommand;
