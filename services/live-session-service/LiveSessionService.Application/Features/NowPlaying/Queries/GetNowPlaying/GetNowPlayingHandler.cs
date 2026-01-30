using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Mappings;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.NowPlaying.Queries.GetNowPlaying;

/// <summary>
/// Handler for getting current now playing
/// Returns from database (cache handled at infrastructure level)
/// </summary>
public sealed class GetNowPlayingHandler : IQueryHandler<GetNowPlayingQuery, NowPlayingResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly INowPlayingHistoryRepository _nowPlayingRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<GetNowPlayingHandler> _logger;

    public GetNowPlayingHandler(
        ILiveSessionRepository sessionRepository,
        INowPlayingHistoryRepository nowPlayingRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<GetNowPlayingHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _nowPlayingRepository = nowPlayingRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<NowPlayingResult>> Handle(
        GetNowPlayingQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting now playing for session {SessionId}", query.SessionId);

            // 1. Get session
            var session = await _sessionRepository.GetByIdAsync(query.SessionId, cancellationToken);
            if (session == null)
            {
                return Result<NowPlayingResult>.Failure("Session not found", 404);
            }

            // 2. Get latest now playing
            var nowPlaying = await _nowPlayingRepository.GetLatestBySessionIdAsync(
                query.SessionId,
                cancellationToken);

            if (nowPlaying == null)
            {
                return Result<NowPlayingResult>.Failure(
                    "No now playing data available for this session",
                    404);
            }

            // 3. Map to Result using mapping extension
            var result = nowPlaying.ToNowPlayingResult(session, _dateTimeProvider);

            return Result<NowPlayingResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get now playing for session {SessionId}", query.SessionId);
            return Result<NowPlayingResult>.Failure(
                "An error occurred while retrieving now playing data",
                500);
        }
    }
}
