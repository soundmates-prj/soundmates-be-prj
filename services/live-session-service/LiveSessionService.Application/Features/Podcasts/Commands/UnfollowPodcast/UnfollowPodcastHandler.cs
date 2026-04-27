using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Podcasts.Commands.UnfollowPodcast;

public sealed class UnfollowPodcastHandler : ICommandHandler<UnfollowPodcastCommand>
{
    private readonly IUserSavedPodcastRepository _userSavedPodcastRepository;

    public UnfollowPodcastHandler(IUserSavedPodcastRepository userSavedPodcastRepository)
    {
        _userSavedPodcastRepository = userSavedPodcastRepository;
    }

    public async Task<Result> Handle(UnfollowPodcastCommand command, CancellationToken cancellationToken)
    {
        var userSavedPodcast = await _userSavedPodcastRepository.GetByUserAndPodcastAsync(
            command.UserId,
            command.PodcastId,
            cancellationToken);

        if (userSavedPodcast == null)
            return Result.Failure("Podcast follow not found", ErrorCode.NotFound);

        await _userSavedPodcastRepository.DeleteAsync(userSavedPodcast, cancellationToken);
        return Result.Success();
    }
}
