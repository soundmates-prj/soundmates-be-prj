using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.PodcastRequests.Queries.GetPodcastRequestById;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.PodcastRequests;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.PodcastRequests.Queries.GetPodcastRequestById;

public sealed class GetPodcastRequestByIdHandler
    : IQueryHandler<GetPodcastRequestByIdQuery, PodcastRequestResult>
{
    private readonly IPodcastRequestRepository _repository;

    public GetPodcastRequestByIdHandler(IPodcastRequestRepository repository) => _repository = repository;

    public async Task<Result<PodcastRequestResult>> Handle(
        GetPodcastRequestByIdQuery query,
        CancellationToken cancellationToken)
    {
        var r = await _repository.GetByIdAsync(query.Id, cancellationToken);
        if (r == null)
            return Result<PodcastRequestResult>.Failure("Podcast request not found", ErrorCode.NotFound);

        return Result<PodcastRequestResult>.Success(new PodcastRequestResult
        {
            Id = r.Id,
            LiveSessionId = r.LiveSessionId,
            RequestedByUserId = r.RequestedByUserId,
            Title = r.Title,
            Description = r.Description,
            ScriptText = r.ScriptText,
            AudioUrl = r.AudioUrl,
            DurationSeconds = r.DurationSeconds,
            VoiceCode = r.VoiceCode,
            VoiceDisplayName = r.VoiceDisplayName,
            AzuraCastMediaId = r.AzuraCastMediaId,
            Status = r.Status.ToString(),
            ReviewedByUserId = r.ReviewedByUserId,
            ReviewedAt = r.ReviewedAt,
            RejectReason = r.RejectReason,
            RequestedAt = r.RequestedAt
        });
    }
}