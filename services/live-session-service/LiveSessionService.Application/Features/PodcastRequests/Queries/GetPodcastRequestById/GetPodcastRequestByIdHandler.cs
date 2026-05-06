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

        return Result<PodcastRequestResult>.Success(new PodcastRequestResult
        {
            Id = r.Id,
            RequestedByUserId = r.RequestedByUserId,
            TargetPodcastId = r.TargetPodcastId,
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
        });
    }
}