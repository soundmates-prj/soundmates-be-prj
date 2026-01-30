using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.NowPlaying;
using LiveSessionService.Application.Mappings;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.NowPlaying.Queries.GetNowPlayingHistory;

/// <summary>
/// Handler for getting now playing history
/// S? d?ng PagedResult ?? support pagination
/// </summary>
public sealed class GetNowPlayingHistoryHandler 
    : IQueryHandler<GetNowPlayingHistoryQuery, PagedResult<NowPlayingHistoryResult>>
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

    public async Task<Result<PagedResult<NowPlayingHistoryResult>>> Handle(
        GetNowPlayingHistoryQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting now playing history for session {SessionId} (Page: {Page}, Size: {Size})",
            query.SessionId,
            query.PageNumber,
            query.PageSize);

        // 1. Validate session exists
        var session = await _sessionRepository.GetByIdAsync(query.SessionId, cancellationToken);
        if (session == null)
        {
            return Result<PagedResult<NowPlayingHistoryResult>>.Failure(
                "Session not found",
                ErrorCode.NotFound);
        }

        // 2. Get history (for now, simple implementation - không phân trang database)
        // TODO: Implement true pagination at repository level
        var limit = query.PageSize;
        if (limit < 1) limit = 1;
        if (limit > 100) limit = 100;

        var history = await _nowPlayingRepository.GetBySessionIdAsync(
            query.SessionId,
            limit,
            cancellationToken);

        // 3. Map to Results
        var results = history.ToHistoryResults();

        // 4. Create paged result (simple unpaged for now)
        var pagedResult = PagedResult<NowPlayingHistoryResult>.CreateUnpaged(results);

        return Result<PagedResult<NowPlayingHistoryResult>>.Success(pagedResult);
    }
}
