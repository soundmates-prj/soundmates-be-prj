using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Mappings;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.NowPlaying.Queries.GetNowPlayingHistory;

/// <summary>
/// Handler for getting now playing history
/// </summary>
public sealed class GetNowPlayingHistoryHandler 
    : IQueryHandler<GetNowPlayingHistoryQuery, List<NowPlayingHistoryResult>>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly INowPlayingHistoryRepository _nowPlayingRepository;
    private readonly ILogger<GetNowPlayingHistoryHandler> _logger;

    public GetNowPlayingHistoryHandler(
        ILiveSessionRepository sessionRepository,
        INowPlayingHistoryRepository nowPlayingRepository,
        ILogger<GetNowPlayingHistoryHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _nowPlayingRepository = nowPlayingRepository;
        _logger = logger;
    }

    public async Task<Result<List<NowPlayingHistoryResult>>> Handle(
        GetNowPlayingHistoryQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Getting now playing history for session {SessionId} (limit: {Limit})",
                query.SessionId,
                query.Limit);

            // 1. Validate session exists
            var session = await _sessionRepository.GetByIdAsync(query.SessionId, cancellationToken);
            if (session == null)
            {
                return Result<List<NowPlayingHistoryResult>>.Failure(
                    "Session not found",
                    404);
            }

            // 2. Get history
            var history = await _nowPlayingRepository.GetBySessionIdAsync(
                query.SessionId,
                query.Limit,
                cancellationToken);

            // 3. Map to Results using mapping extensions
            var results = history.ToHistoryResults();

            return Result<List<NowPlayingHistoryResult>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get history for session {SessionId}", query.SessionId);
            return Result<List<NowPlayingHistoryResult>>.Failure(
                "An error occurred while retrieving history",
                500);
        }
    }
}
                500);
        }
    }
}
