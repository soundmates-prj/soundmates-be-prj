using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetAllSessionSchedules;

public sealed record GetAllSessionSchedulesQuery(Guid? LiveSessionId) : IQuery<List<SessionScheduleResult>>;
