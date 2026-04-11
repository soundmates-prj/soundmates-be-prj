using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.PodcastRequests.Commands.CancelPodcastRequest;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.PodcastRequests.Commands.CancelPodcastRequest;

public sealed class CancelPodcastRequestHandler
    : ICommandHandler<CancelPodcastRequestCommand, bool>
{
    private readonly IPodcastRequestRepository _repository;

    public CancelPodcastRequestHandler(IPodcastRequestRepository repository)
        => _repository = repository;

    public async Task<Result<bool>> Handle(
        CancelPodcastRequestCommand command,
        CancellationToken cancellationToken)
    {
        var r = await _repository.GetByIdAsync(command.PodcastRequestId, cancellationToken);
        if (r == null)
            return Result<bool>.Failure("Podcast request not found", ErrorCode.NotFound);

        if (r.RequestedByUserId != command.UserId)
            return Result<bool>.Failure("You can only cancel your own requests", ErrorCode.Forbidden);

        if (r.Status != PodcastRequestStatus.Pending)
            return Result<bool>.Failure("Only pending requests can be cancelled", ErrorCode.BadRequest);

        r.Status = PodcastRequestStatus.Cancelled;
        await _repository.UpdateAsync(r, cancellationToken);

        return Result<bool>.Success(true);
    }
}