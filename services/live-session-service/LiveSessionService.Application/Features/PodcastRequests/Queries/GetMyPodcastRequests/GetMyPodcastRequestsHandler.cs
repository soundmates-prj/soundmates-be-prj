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