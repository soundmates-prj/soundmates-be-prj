using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.PodcastRequests;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.PodcastEpisodeRequests.Queries.GetPodcastEpisodeRequestById;

public sealed class GetPodcastEpisodeRequestByIdHandler
    : IQueryHandler<GetPodcastEpisodeRequestByIdQuery, PodcastEpisodeRequestResult>
{
    private readonly IPodcastEpisodeRequestRepository _repository;

    public GetPodcastEpisodeRequestByIdHandler(IPodcastEpisodeRequestRepository repository) => _repository = repository;

    public async Task<Result<PodcastEpisodeRequestResult>> Handle(
        GetPodcastEpisodeRequestByIdQuery query,
        CancellationToken cancellationToken)
    {
        var r = await _repository.GetByIdAsync(query.Id, cancellationToken);
        if (r == null)
            return Result<PodcastEpisodeRequestResult>.Failure("Episode request not found", ErrorCode.NotFound);

        return Result<PodcastEpisodeRequestResult>.Success(new PodcastEpisodeRequestResult
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
        });
    }
}
