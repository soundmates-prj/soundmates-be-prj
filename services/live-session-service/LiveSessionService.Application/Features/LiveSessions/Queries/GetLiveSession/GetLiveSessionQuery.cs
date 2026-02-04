using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSession;

public sealed record GetLiveSessionQuery(Guid SessionId) : IQuery<LiveSessionResult>;
