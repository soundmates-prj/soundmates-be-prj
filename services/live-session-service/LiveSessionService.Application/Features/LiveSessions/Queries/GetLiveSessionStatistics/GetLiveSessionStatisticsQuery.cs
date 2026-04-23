using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSessionStatistics;

public sealed record GetLiveSessionStatisticsQuery(Guid SessionId) : IQuery<LiveSessionStatisticsResult>;
