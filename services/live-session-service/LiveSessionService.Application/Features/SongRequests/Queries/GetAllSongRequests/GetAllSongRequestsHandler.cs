using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.SongRequests;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.SongRequests.Queries.GetAllSongRequests;

public sealed class GetAllSongRequestsHandler : IQueryHandler<GetAllSongRequestsQuery, PagedResult<SongRequestResult>>
{
    private readonly ISongRequestRepository _songRequestRepository;

    public GetAllSongRequestsHandler(ISongRequestRepository songRequestRepository)
    {
        _songRequestRepository = songRequestRepository;
    }

    public async Task<Result<PagedResult<SongRequestResult>>> Handle(GetAllSongRequestsQuery query, CancellationToken cancellationToken)
    {
        SongRequestStatus? status = null;

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<SongRequestStatus>(query.Status, true, out var parsedStatus))
                return Result<PagedResult<SongRequestResult>>.Failure("Invalid song request status", ErrorCode.BadRequest);

            status = parsedStatus;
        }

        var (items, totalCount) = await _songRequestRepository.GetAllAsync(status, query.Page, query.PageSize, cancellationToken);

        var resultItems = items.Select(x => new SongRequestResult
        {
            Id = x.Id,
            LiveSessionId = x.LiveSessionId,
            MediaFileId = x.MediaFileId,
            RequestedByUserId = x.RequestedByUserId,
            Status = x.Status.ToString(),
            ReviewedByUserId = x.ReviewedByUserId,
            RequestedAt = x.RequestedAt,
            ReviewedAt = x.ReviewedAt,
            Message = x.Message,
            RejectReason = x.RejectReason,
            SongTitle = x.MediaFile?.Title ?? "Unknown Song",
            SongArtist = x.MediaFile?.Artist,
            SongAlbum = x.MediaFile?.Album
        }).ToList();

        var pagedResult = new PagedResult<SongRequestResult>(resultItems, totalCount, query.Page, query.PageSize);
        return Result<PagedResult<SongRequestResult>>.Success(pagedResult);
    }
}
