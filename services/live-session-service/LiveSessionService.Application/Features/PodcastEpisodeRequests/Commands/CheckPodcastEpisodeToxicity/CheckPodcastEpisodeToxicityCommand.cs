using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.PodcastEpisodeRequests.Commands.CheckPodcastEpisodeToxicity;

public record CheckPodcastEpisodeToxicityCommand(Guid RequestId) : ICommand<ModerationResult>;
