using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.NowPlaying;

namespace LiveSessionService.Application.Features.NowPlaying.Queries.GetNowPlayingHistory;

/// <summary>
/// Query to get now playing history for a session
/// S? d?ng PagedResult ?? s?n sàng cho pagination
/// </summary>
public sealed record GetNowPlayingHistoryQuery(
    Guid SessionId,
    int PageNumber = 1,
    int PageSize = 50) : IQuery<PagedResult<NowPlayingHistoryResult>>;
