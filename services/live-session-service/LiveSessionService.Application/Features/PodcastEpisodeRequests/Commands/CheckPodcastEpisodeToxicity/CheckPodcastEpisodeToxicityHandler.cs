using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace LiveSessionService.Application.Features.PodcastEpisodeRequests.Commands.CheckPodcastEpisodeToxicity;

public sealed class CheckPodcastEpisodeToxicityHandler : ICommandHandler<CheckPodcastEpisodeToxicityCommand, ModerationResult>
{
    private readonly IPodcastEpisodeRequestRepository _repository;
    private readonly IAudioModerationService _moderationService;

    public CheckPodcastEpisodeToxicityHandler(
        IPodcastEpisodeRequestRepository repository,
        IAudioModerationService moderationService)
    {
        _repository = repository;
        _moderationService = moderationService;
    }

    public async Task<Result<ModerationResult>> Handle(CheckPodcastEpisodeToxicityCommand command, CancellationToken cancellationToken)
    {
        var request = await _repository.GetByIdAsync(command.RequestId, cancellationToken);
        if (request == null)
            return Result<ModerationResult>.Failure("Episode request not found", ErrorCode.NotFound);

        if (string.IsNullOrWhiteSpace(request.AudioUrl))
            return Result<ModerationResult>.Failure("Episode request has no audio URL", ErrorCode.BadRequest);

        var moderationResult = await _moderationService.CheckToxicityAsync(request.AudioUrl, cancellationToken);

        return Result<ModerationResult>.Success(moderationResult);
    }
}
