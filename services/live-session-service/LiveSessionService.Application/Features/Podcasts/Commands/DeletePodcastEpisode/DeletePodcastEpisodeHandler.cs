using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Podcasts.Commands.DeletePodcastEpisode;

public sealed class DeletePodcastEpisodeHandler : ICommandHandler<DeletePodcastEpisodeCommand>
{
    private readonly IPodcastRepository _podcastRepository;

    public DeletePodcastEpisodeHandler(IPodcastRepository podcastRepository)
    {
        _podcastRepository = podcastRepository;
    }

    public async Task<Result> Handle(DeletePodcastEpisodeCommand command, CancellationToken cancellationToken)
    {
        var podcast = await _podcastRepository.GetByIdAsync(command.PodcastId, cancellationToken);
        if (podcast == null)
        {
            return Result.Failure("Podcast not found", ErrorCode.NotFound);
        }

        var episode = await _podcastRepository.GetEpisodeByIdAsync(command.EpisodeId, cancellationToken);
        if (episode == null || episode.PodcastId != command.PodcastId)
        {
            return Result.Failure("Podcast episode not found", ErrorCode.NotFound);
        }

        await _podcastRepository.DeleteEpisodeAsync(episode, cancellationToken);

        return Result.Success();
    }
}
