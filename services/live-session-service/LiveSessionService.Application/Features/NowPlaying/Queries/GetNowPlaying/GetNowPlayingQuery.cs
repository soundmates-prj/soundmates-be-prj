using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;

namespace LiveSessionService.Application.Features.NowPlaying.Queries.GetNowPlaying;

/// <summary>
/// Query to get current now playing for a session
/// Returns cached data if available, otherwise from database
/// </summary>
public sealed record GetNowPlayingQuery(Guid SessionId) : IQuery<NowPlayingResult>;
