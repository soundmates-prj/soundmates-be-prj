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

        var results = await _repository.GetAllAsync(status, cancellationToken);

        // Apply search filter in memory
        if (!string.IsNullOrWhiteSpace(query.SearchQuery))
        {
            var q = query.SearchQuery.ToLowerInvariant();
            results = results.Where(r =>
                r.Title.ToLowerInvariant().Contains(q) ||
                (r.AuthorInfo?.ToLowerInvariant().Contains(q) ?? false) ||
                r.Type.ToLowerInvariant().Contains(q))
                .ToList();
        }

        var mapped = results.Select(r => new PodcastRequestResult
        {
            Id = r.Id,
            RequestedByUserId = r.RequestedByUserId,
            AuthorInfo = string.IsNullOrWhiteSpace(r.AuthorInfo) ? null : System.Text.Json.JsonSerializer.Deserialize<object>(r.AuthorInfo),
            Title = r.Title,
            Type = r.Type,
            Description = r.Description,
            BannerUrl = r.BannerUrl,
            Price = r.Price,
            IsPaid = r.IsPaid,
            Status = r.Status.ToString(),
            ReviewedByUserId = r.ReviewedByUserId,
            ReviewedAt = r.ReviewedAt,
            RejectReason = r.RejectReason,
            RequestedAt = r.RequestedAt
        }).ToList();

        return Result<List<PodcastRequestResult>>.Success(mapped);
    }
}