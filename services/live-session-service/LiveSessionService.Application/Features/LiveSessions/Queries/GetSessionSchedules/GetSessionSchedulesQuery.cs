using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetSessionSchedules;

public sealed record GetSessionSchedulesQuery(Guid LiveSessionId) : IQuery<List<SessionScheduleResult>>;
