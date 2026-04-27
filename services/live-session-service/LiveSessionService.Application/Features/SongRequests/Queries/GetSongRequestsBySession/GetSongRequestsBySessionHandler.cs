using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.SongRequests;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.SongRequests.Queries.GetSongRequestsBySession;

public sealed class GetSongRequestsBySessionHandler : IQueryHandler<GetSongRequestsBySessionQuery, List<SongRequestResult>>
{
    private readonly ILiveSessionRepository _liveSessionRepository;
    private readonly ISongRequestRepository _songRequestRepository;

    public GetSongRequestsBySessionHandler(
        ILiveSessionRepository liveSessionRepository,
        ISongRequestRepository songRequestRepository)
    {
        _liveSessionRepository = liveSessionRepository;
        _songRequestRepository = songRequestRepository;
    }

    public async Task<Result<List<SongRequestResult>>> Handle(
        GetSongRequestsBySessionQuery query,
        CancellationToken cancellationToken)
    {
        var liveSession = await _liveSessionRepository.GetByIdAsync(query.LiveSessionId, cancellationToken);
        if (liveSession == null)
            return Result<List<SongRequestResult>>.Failure("Live session not found", ErrorCode.NotFound);

        SongRequestStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<SongRequestStatus>(query.Status, true, out var parsedStatus))
                return Result<List<SongRequestResult>>.Failure("Invalid song request status", ErrorCode.BadRequest);

            status = parsedStatus;
        }

        var requests = await _songRequestRepository.GetByLiveSessionIdAsync(
            query.LiveSessionId,
            status,
            cancellationToken);

        var result = requests.Select(x => new SongRequestResult
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
            SongTitle = x.MediaFile.Title,
            SongArtist = x.MediaFile.Artist,
            SongAlbum = x.MediaFile.Album
        }).ToList();

        return Result<List<SongRequestResult>>.Success(result);
    }
}
