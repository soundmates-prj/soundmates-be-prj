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
                (r.EpisodeTitle?.ToLowerInvariant().Contains(q) ?? false))
                .ToList();
        }

        var mapped = results.Select(r =>
        {
            object? parsedAuthorInfo = null;
            if (!string.IsNullOrWhiteSpace(r.AuthorInfo))
            {
                var a = r.AuthorInfo.Trim();
                if (a.Length > 0 && (a[0] == '{' || a[0] == '[' || a[0] == '"'))
                {
                    try { parsedAuthorInfo = System.Text.Json.JsonSerializer.Deserialize<object>(a); }
                    catch (Exception) { parsedAuthorInfo = a; }
                }
                else { parsedAuthorInfo = a; }
            }

            return new PodcastRequestResult
            {
                Id = r.Id,
                RequestedByUserId = r.RequestedByUserId,
                AuthorInfo = parsedAuthorInfo,
                Title = r.Title,
                EpisodeTitle = r.EpisodeTitle,
                Description = r.Description,
                BannerUrl = r.BannerUrl,
                AudioUrl = r.AudioUrl,
                Price = r.Price,
                IsPaid = r.IsPaid,
                Status = r.Status.ToString(),
                ReviewedByUserId = r.ReviewedByUserId,
                ReviewedAt = r.ReviewedAt,
                RejectReason = r.RejectReason,
                RequestedAt = r.RequestedAt
            };
        }).ToList();

        return Result<List<PodcastRequestResult>>.Success(mapped);
    }
}