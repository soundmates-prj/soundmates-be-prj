using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetActiveLiveSessions;

public sealed record GetActiveLiveSessionsQuery() : IQuery<List<LiveSessionResult>>;
