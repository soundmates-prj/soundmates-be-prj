using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.PodcastRequests.Queries.GetPodcastRequests;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.PodcastRequests;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.PodcastRequests.Queries.GetPodcastRequests;

public sealed class GetPodcastRequestsHandler
    : IQueryHandler<GetPodcastRequestsQuery, List<PodcastRequestResult>>
{
    private readonly IPodcastRequestRepository _repository;

    public GetPodcastRequestsHandler(IPodcastRequestRepository repository) => _repository = repository;

    public async Task<Result<List<PodcastRequestResult>>> Handle(
        GetPodcastRequestsQuery query,
        CancellationToken cancellationToken)
    {
        PodcastRequestStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<PodcastRequestStatus>(query.Status, true, out var parsedStatus))
        {
            status = parsedStatus;
        }

        var results = await _repository.GetAllAsync(query.LiveSessionId, status, cancellationToken);

        var mapped = results.Select(r => new PodcastRequestResult
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
        }).ToList();

        // Apply search filter in memory (for username search - would need join in real query)
        if (!string.IsNullOrWhiteSpace(query.SearchQuery))
        {
            var q = query.SearchQuery.ToLowerInvariant();
            mapped = mapped.Where(r =>
                r.Title.ToLowerInvariant().Contains(q) ||
                (r.VoiceDisplayName?.ToLowerInvariant().Contains(q) ?? false))
                .ToList();
        }

        return Result<List<PodcastRequestResult>>.Success(mapped);
    }
}