using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;

namespace LiveSessionService.Application.Features.NowPlaying.Queries.GetNowPlayingHistory;

/// <summary>
/// Query to get now playing history for a session
/// </summary>
public sealed record GetNowPlayingHistoryQuery(
    Guid SessionId,
    int Limit = 50) : IQuery<List<NowPlayingHistoryResult>>;
