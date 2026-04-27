using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.NowPlaying;
using LiveSessionService.Application.Mappings;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.NowPlaying.Commands.SyncNowPlaying;

/// <summary>
/// Handler for syncing now playing data from AzuraCast
/// 
/// RESPONSIBILITIES:
/// 1. Fetch data from AzuraCast API
/// 2. Create/update NowPlayingHistory entity
/// 3. Save to database
/// 4. Enqueue outbox message for event publishing
/// 
/// NOTE: Chỉ handle business errors (DomainException)
/// System errors (Exception) để middleware xử lý
/// </summary>
public sealed class SyncNowPlayingHandler : ICommandHandler<SyncNowPlayingCommand, NowPlayingResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly INowPlayingHistoryRepository _nowPlayingRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly IOutboxRepository _outbox;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<SyncNowPlayingHandler> _logger;

    public SyncNowPlayingHandler(
        ILiveSessionRepository sessionRepository,
        INowPlayingHistoryRepository nowPlayingRepository,
        IAzuraCastClient azuraCastClient,
        IOutboxRepository outbox,
        IDateTimeProvider dateTimeProvider,
        ILogger<SyncNowPlayingHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _nowPlayingRepository = nowPlayingRepository;
        _azuraCastClient = azuraCastClient;
        _outbox = outbox;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<NowPlayingResult>> Handle(
        SyncNowPlayingCommand command,
        CancellationToken cancellationToken)
    {
        // Chỉ catch DomainException (business errors)
        // System errors throw lên để middleware handle
        try
        {
            _logger.LogInformation("Syncing now playing for session {SessionId}", command.SessionId);

            // 1. Get session with station
            var session = await _sessionRepository.GetByIdWithStationAsync(command.SessionId, cancellationToken);
            if (session == null)
            {
                return Result<NowPlayingResult>.Failure("Session not found", ErrorCode.NotFound);
            }

            // 2. Validate session is active
            if (!session.IsActive())
            {
                return Result<NowPlayingResult>.Failure("Session is not active", ErrorCode.BadRequest);
            }

            // 3. Validate station exists
            if (session.AzuraCastStation == null || !session.AzuraCastStationId.HasValue)
            {
                return Result<NowPlayingResult>.Failure(
                    "Session does not have an AzuraCast station configured",
                    ErrorCode.BadRequest);
            }

            var station = session.AzuraCastStation;

            // 4. Validate station is enabled
            if (!station.IsEnabled)
            {
                return Result<NowPlayingResult>.Failure(
                    "AzuraCast station is disabled", 
                    ErrorCode.BadRequest);
            }

            // 5. Fetch from AzuraCast API
            var nowPlayingData = await _azuraCastClient.GetNowPlayingAsync(
                station.ExternalStationId, 
                cancellationToken);

            if (nowPlayingData?.NowPlaying?.Song == null)
            {
                _logger.LogWarning("No now playing data from AzuraCast for session {SessionId}", command.SessionId);
                return Result<NowPlayingResult>.Failure(
                    "No now playing data available from AzuraCast",
                    ErrorCode.NotFound);
            }

            // 6. Get previous now playing to check if song changed
            var previousNowPlaying = await _nowPlayingRepository.GetLatestBySessionIdAsync(
                command.SessionId,
                cancellationToken);

            var currentSong = nowPlayingData.NowPlaying.Song;
            var currentShId = nowPlayingData.NowPlaying.ShId;

            // 7. Check if this is a new song
            bool isNewSong = previousNowPlaying == null ||
                             previousNowPlaying.AzuraCastSongHistoryId != currentShId;

            NowPlayingHistory nowPlayingHistory;

            if (isNewSong)
            {
                // Mark previous song as ended
                if (previousNowPlaying != null && previousNowPlaying.IsCurrentlyPlaying())
                {
                    previousNowPlaying.MarkEnded(_dateTimeProvider);
                    await _nowPlayingRepository.UpdateAsync(previousNowPlaying, cancellationToken);
                }

                // Create new now playing entry
                // Note: PlayedAt is double (fractional seconds) — truncate to long for Unix timestamp conversion
                var playedAtTimestamp = (long)nowPlayingData.NowPlaying.PlayedAt;
                var playedAt = DateTimeOffset.FromUnixTimeSeconds(playedAtTimestamp).UtcDateTime;

                nowPlayingHistory = NowPlayingHistory.Create(
                    liveSessionId: session.Id,
                    songTitle: currentSong.Title ?? currentSong.Text ?? "Unknown",
                    songArtist: currentSong.Artist,
                    songAlbum: currentSong.Album,
                    songArtUrl: currentSong.Art,
                    lyrics: currentSong.Lyrics,
                    durationSeconds: (int)nowPlayingData.NowPlaying.Duration,
                    playedAt: playedAt,
                    listenerCount: nowPlayingData.Listeners?.Current ?? 0,
                    azuraCastSongHistoryId: currentShId,
                    isRequest: false,
                    requestedByUserId: null,
                    dateTimeProvider: _dateTimeProvider);

                await _nowPlayingRepository.AddAsync(nowPlayingHistory, cancellationToken);

                _logger.LogInformation(
                    "New song detected for session {SessionId}: {SongTitle} by {Artist}",
                    session.Id,
                    nowPlayingHistory.SongTitle,
                    nowPlayingHistory.SongArtist);

                // Enqueue outbox message for new song
                await _outbox.EnqueueAsync("livesession.nowplaying.song_changed", new
                {
                    sessionId = session.Id,
                    sessionName = session.SessionName,
                    songId = nowPlayingHistory.Id,
                    songTitle = nowPlayingHistory.SongTitle,
                    songArtist = nowPlayingHistory.SongArtist,
                    songAlbum = nowPlayingHistory.SongAlbum,
                    songArtUrl = nowPlayingHistory.SongArtUrl,
                    playedAt = nowPlayingHistory.PlayedAt,
                    listenerCount = nowPlayingHistory.ListenerCount
                }, cancellationToken);
            }
            else
            {
                // Update listener count for existing song
                nowPlayingHistory = previousNowPlaying!;
                var currentListeners = nowPlayingData.Listeners?.Current ?? 0;
                nowPlayingHistory.UpdateListenerPeak(currentListeners);
                await _nowPlayingRepository.UpdateAsync(nowPlayingHistory, cancellationToken);
            }

            // 8. Update station sync status
            station.MarkSyncSuccessful(_dateTimeProvider);

            // 9. Map to Result
            var result = nowPlayingHistory.ToNowPlayingResult(session, _dateTimeProvider);

            return Result<NowPlayingResult>.Success(result);
        }
        catch (DomainException ex)
        {
            // Business logic errors - convert to Result
            _logger.LogWarning(ex, "Domain validation failed for session {SessionId}", command.SessionId);
            return Result<NowPlayingResult>.Failure(ex.Message, (ErrorCode)ex.StatusCode);
        }
        // System errors (Exception) throw lên để GlobalExceptionMiddleware handle
    }
}
