using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.NowPlaying;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSessionNowPlaying;

/// <summary>
/// Query to get real-time now playing data for a live session from AzuraCast
/// </summary>
public sealed record GetLiveSessionNowPlayingQuery(Guid SessionId) : IQuery<StationNowPlayingResult>;
