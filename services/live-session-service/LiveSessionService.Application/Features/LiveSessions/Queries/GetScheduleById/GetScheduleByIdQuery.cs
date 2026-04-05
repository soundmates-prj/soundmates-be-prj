using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetScheduleById;

public sealed record GetScheduleByIdQuery(Guid ScheduleId) : IQuery<SessionScheduleResult>;
