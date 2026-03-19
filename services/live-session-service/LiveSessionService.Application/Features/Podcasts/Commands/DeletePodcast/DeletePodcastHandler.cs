using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Podcasts.Commands.DeletePodcast;

public sealed class DeletePodcastHandler : ICommandHandler<DeletePodcastCommand>
{
    private readonly IPodcastRepository _podcastRepository;

    public DeletePodcastHandler(IPodcastRepository podcastRepository)
    {
        _podcastRepository = podcastRepository;
    }

    public async Task<Result> Handle(DeletePodcastCommand command, CancellationToken cancellationToken)
    {
        var podcast = await _podcastRepository.GetByIdAsync(command.PodcastId, cancellationToken);
        if (podcast == null)
            return Result.Failure("Podcast not found", ErrorCode.NotFound);

        await _podcastRepository.DeleteAsync(podcast, cancellationToken);

        return Result.Success();
    }
}
