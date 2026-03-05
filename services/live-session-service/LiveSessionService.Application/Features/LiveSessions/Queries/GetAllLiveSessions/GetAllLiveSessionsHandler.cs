using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetAllLiveSessions;

public sealed class GetAllLiveSessionsHandler : IQueryHandler<GetAllLiveSessionsQuery, PagedResult<LiveSessionResult>>
{
    private readonly ILiveSessionRepository _sessionRepository;

    public GetAllLiveSessionsHandler(ILiveSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<PagedResult<LiveSessionResult>>> Handle(
        GetAllLiveSessionsQuery query,
        CancellationToken cancellationToken)
    {
        // For now, simple implementation without pagination
        var sessions = await _sessionRepository.GetAllWithStationsAsync(cancellationToken);

        // Filter by userId if provided
        if (query.UserId.HasValue)
        {
            sessions = sessions.Where(s => s.HostUserId == query.UserId.Value).ToList();
        }

        // Filter by status if provided
        if (!string.IsNullOrEmpty(query.Status))
        {
            sessions = sessions.Where(s => s.Status.ToString().Equals(query.Status, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var results = sessions.Select(s => new LiveSessionResult
        {
            Id = s.Id,
            UserId = s.HostUserId,
            StationId = s.AzuraCastStationId!.Value,
            StationName = s.AzuraCastStation?.StationName,
            SessionName = s.SessionName,
            Description = s.Description,
            Status = s.Status.ToString(),
            StartedAt = s.StartedAt,
            EndedAt = s.EndedAt,
            CreatedAt = s.CreatedAt
        }).ToList();

        var pagedResult = PagedResult<LiveSessionResult>.CreateUnpaged(results);
        return Result<PagedResult<LiveSessionResult>>.Success(pagedResult);
    }
}
