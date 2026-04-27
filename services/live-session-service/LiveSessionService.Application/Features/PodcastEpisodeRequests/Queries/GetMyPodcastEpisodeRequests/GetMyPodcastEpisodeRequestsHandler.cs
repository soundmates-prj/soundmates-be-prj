using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.PodcastRequests;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.PodcastEpisodeRequests.Queries.GetMyPodcastEpisodeRequests;

public sealed class GetMyPodcastEpisodeRequestsHandler
    : IQueryHandler<GetMyPodcastEpisodeRequestsQuery, List<PodcastEpisodeRequestResult>>
{
    private readonly IPodcastEpisodeRequestRepository _repository;

    public GetMyPodcastEpisodeRequestsHandler(IPodcastEpisodeRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<PodcastEpisodeRequestResult>>> Handle(
        GetMyPodcastEpisodeRequestsQuery query,
        CancellationToken cancellationToken)
    {
        PodcastRequestStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<PodcastRequestStatus>(query.Status, true, out var parsedStatus))
            {
                return Result<List<PodcastEpisodeRequestResult>>.Failure(
                    "Invalid status filter. Allowed values: Pending, Approved, Rejected",
                    ErrorCode.BadRequest);
            }
            status = parsedStatus;
        }

        var results = await _repository.GetByUserIdAsync(query.UserId, status, cancellationToken);

        var mapped = results.Select(r => new PodcastEpisodeRequestResult
        {
            Id = r.Id,
            PodcastId = r.PodcastId,
            RequestedByUserId = r.RequestedByUserId,
            AuthorInfo = string.IsNullOrWhiteSpace(r.AuthorInfo) ? null : System.Text.Json.JsonSerializer.Deserialize<object>(r.AuthorInfo),
            Title = r.Title,
            Description = r.Description,
            ThumbnailUrl = r.ThumbnailUrl,
            AudioUrl = r.AudioUrl,
            Duration = r.Duration,
            Status = r.Status.ToString(),
            ReviewedByUserId = r.ReviewedByUserId,
            ReviewedAt = r.ReviewedAt,
            RejectReason = r.RejectReason,
            RequestedAt = r.RequestedAt
        }).ToList();

        return Result<List<PodcastEpisodeRequestResult>>.Success(mapped);
    }
}
