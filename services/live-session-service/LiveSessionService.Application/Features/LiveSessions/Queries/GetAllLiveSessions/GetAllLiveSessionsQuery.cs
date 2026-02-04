using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetAllLiveSessions;

public sealed record GetAllLiveSessionsQuery(
    Guid? UserId = null,
    string? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedResult<LiveSessionResult>>;
