using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetActiveLiveSessions;

public sealed class GetActiveLiveSessionsHandler : IQueryHandler<GetActiveLiveSessionsQuery, List<LiveSessionResult>>
{
    private readonly ILiveSessionRepository _sessionRepository;

    public GetActiveLiveSessionsHandler(ILiveSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<List<LiveSessionResult>>> Handle(
        GetActiveLiveSessionsQuery query,
        CancellationToken cancellationToken)
    {
        var sessions = await _sessionRepository.GetActiveSessionsAsync(cancellationToken);

        var results = sessions.Select(s => new LiveSessionResult
        {
            Id = s.Id,
            UserId = s.HostUserId,
            StationId = s.AzuraCastStationId ?? Guid.Empty,
            StationName = s.AzuraCastStation?.StationName,
            SessionName = s.SessionName,
            Description = s.Description,
            Status = s.Status.ToString(),
            StartedAt = s.StartedAt,
            EndedAt = s.EndedAt,
            CreatedAt = s.CreatedAt,
            StreamUrl = s.AzuraCastStation?.StreamUrl,
            StationShortcode = s.AzuraCastStation?.StationShortcode,
            PublicPlayerUrl = s.AzuraCastStation?.PublicPlayerUrl,
            ThumbnailUrl = s.ThumbnailUrl,
            Genre = s.Genre,
            ListenersCount = s.Listeners == null
                ? 0
                : s.Listeners
                    .Where(l => l.IsConnected)
                    .Select(l => l.UserId.HasValue
                        ? $"u:{l.UserId.Value}"
                        : $"a:{l.AnonymousIdentifier ?? l.Id.ToString()}")
                    .Distinct()
                    .Count()
        }).ToList();

        return Result<List<LiveSessionResult>>.Success(results);
    }
}
