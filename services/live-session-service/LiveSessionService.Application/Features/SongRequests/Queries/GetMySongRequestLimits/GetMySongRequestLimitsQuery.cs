using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.SongRequests;

namespace LiveSessionService.Application.Features.SongRequests.Queries.GetMySongRequestLimits;

public sealed record GetMySongRequestLimitsQuery(
    Guid UserId,
    string UserToken) : IQuery<MySongRequestLimitsResult>;

public sealed record MySongRequestLimitsResult(
    int Limit,
    int UsedToday,
    int Remaining);
