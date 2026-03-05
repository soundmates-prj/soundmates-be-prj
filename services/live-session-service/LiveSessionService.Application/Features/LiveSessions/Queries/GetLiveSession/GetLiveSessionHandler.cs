using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSession;

public sealed class GetLiveSessionHandler : IQueryHandler<GetLiveSessionQuery, LiveSessionResult>
{
    private readonly ILiveSessionRepository _sessionRepository;

    public GetLiveSessionHandler(ILiveSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<LiveSessionResult>> Handle(
        GetLiveSessionQuery query,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdWithStationAsync(query.SessionId, cancellationToken);
        
        if (session == null)
        {
            return Result<LiveSessionResult>.Failure(
                "Session not found",
                ErrorCode.NotFound);
        }

        var result = new LiveSessionResult
        {
            Id = session.Id,
            UserId = session.HostUserId,
            StationId = session.AzuraCastStationId!.Value,
            StationName = session.AzuraCastStation?.StationName,
            SessionName = session.SessionName,
            Description = session.Description,
            Status = session.Status.ToString(),
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            CreatedAt = session.CreatedAt
        };

        return Result<LiveSessionResult>.Success(result);
    }
}
