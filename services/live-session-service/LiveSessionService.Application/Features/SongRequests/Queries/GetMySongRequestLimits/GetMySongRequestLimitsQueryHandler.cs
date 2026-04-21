using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.SongRequests.Queries.GetMySongRequestLimits;

public sealed class GetMySongRequestLimitsQueryHandler : IQueryHandler<GetMySongRequestLimitsQuery, MySongRequestLimitsResult>
{
    private readonly ISongRequestRepository _songRequestRepository;
    private readonly IAccountContentClient _accountClient;

    public GetMySongRequestLimitsQueryHandler(
        ISongRequestRepository songRequestRepository,
        IAccountContentClient accountClient)
    {
        _songRequestRepository = songRequestRepository;
        _accountClient = accountClient;
    }

    public async Task<Result<MySongRequestLimitsResult>> Handle(GetMySongRequestLimitsQuery query, CancellationToken cancellationToken)
    {
        var subscription = await _accountClient.GetMySubscriptionFullAsync(query.UserToken, cancellationToken);
        var requestLimit = subscription?.RequestLimit ?? 0;
        
        var todayCount = await _songRequestRepository.CountRequestsByUserTodayAsync(query.UserId, cancellationToken);
        
        var remaining = Math.Max(0, requestLimit - todayCount);

        return Result<MySongRequestLimitsResult>.Success(new MySongRequestLimitsResult(
            requestLimit,
            todayCount,
            remaining
        ));
    }
}
