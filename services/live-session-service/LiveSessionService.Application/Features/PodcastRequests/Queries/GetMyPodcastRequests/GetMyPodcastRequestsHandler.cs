using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.PodcastRequests.Queries.GetMyPodcastRequests;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.PodcastRequests;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.PodcastRequests.Queries.GetMyPodcastRequests;

public sealed class GetMyPodcastRequestsHandler
    : IQueryHandler<GetMyPodcastRequestsQuery, List<PodcastRequestResult>>
{
    private readonly IPodcastRequestRepository _repository;

    public GetMyPodcastRequestsHandler(IPodcastRequestRepository repository) => _repository = repository;

    public async Task<Result<List<PodcastRequestResult>>> Handle(
        GetMyPodcastRequestsQuery query,
        CancellationToken cancellationToken)
    {
        var status = !string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<PodcastRequestStatus>(query.Status, true, out var s) ? s : (PodcastRequestStatus?)null;

        var results = await _repository.GetAllAsync(status, cancellationToken);
        results = results.Where(r => r.RequestedByUserId == query.UserId).ToList();

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