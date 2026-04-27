using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.NowPlaying;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSessionQueue;

/// <summary>
/// Query to get real-time upcoming queue data for a live session from AzuraCast
/// </summary>
public sealed record GetLiveSessionQueueQuery(Guid SessionId) : IQuery<LiveSessionQueueResult>;
