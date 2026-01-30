using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Common;
using LiveSessionService.Application.Features.Results;
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
/// </summary>
public sealed class SyncNowPlayingHandler : ICommandHandler<SyncNowPlayingCommand, NowPlayingResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly INowPlayingHistoryRepository _nowPlayingRepository;
    private readonly IAzuraCastService _azuraCastService;
    private readonly IOutboxRepository _outbox;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<SyncNowPlayingHandler> _logger;

    public SyncNowPlayingHandler(
        ILiveSessionRepository sessionRepository,
        INowPlayingHistoryRepository nowPlayingRepository,
        IAzuraCastService azuraCastService,
        IOutboxRepository outbox,
        IDateTimeProvider dateTimeProvider,
        ILogger<SyncNowPlayingHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _nowPlayingRepository = nowPlayingRepository;
        _azuraCastService = azuraCastService;
        _outbox = outbox;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<NowPlayingResult>> Handle(
        SyncNowPlayingCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Syncing now playing for session {SessionId}", command.SessionId);

            // 1. Get session with station
            var session = await _sessionRepository.GetByIdWithStationAsync(command.SessionId, cancellationToken);
            if (session == null)
            {
                return Result<NowPlayingResult>.Failure("Session not found", 404);
            }

            // 2. Validate session is active
            if (!session.IsActive())
            {
                return Result<NowPlayingResult>.Failure("Session is not active", 400);
            }

            // 3. Validate station exists
            if (session.AzuraCastStation == null || !session.AzuraCastStationId.HasValue)
            {
                return Result<NowPlayingResult>.Failure(
                    "Session does not have an AzuraCast station configured",
                    400);
            }

            var station = session.AzuraCastStation;

            // 4. Validate station is enabled
            if (!station.IsEnabled)
            {
                return Result<NowPlayingResult>.Failure("AzuraCast station is disabled", 400);
            }

            // 5. Validate API configuration
            if (string.IsNullOrWhiteSpace(station.ApiBaseUrl))
            {
                return Result<NowPlayingResult>.Failure(
                    "AzuraCast station API base URL is not configured",
                    400);
            }

            // 6. Fetch from AzuraCast API (using ExternalStationId which is integer)
            var nowPlayingData = await _azuraCastService.GetNowPlayingAsync(
                station.ApiBaseUrl,
                station.ExternalStationId, // This is int (1, 2, 3...)
                cancellationToken);

            if (nowPlayingData?.NowPlaying?.Song == null)
            {
                _logger.LogWarning("No now playing data from AzuraCast for session {SessionId}", command.SessionId);
                return Result<NowPlayingResult>.Failure(
                    "No now playing data available from AzuraCast",
                    404);
            }

            // 7. Get previous now playing to check if song changed
            var previousNowPlaying = await _nowPlayingRepository.GetLatestBySessionIdAsync(
                command.SessionId,
                cancellationToken);

            var currentSong = nowPlayingData.NowPlaying.Song;
            var currentShId = nowPlayingData.NowPlaying.ShId;

            // 8. Check if this is a new song
            bool isNewSong = previousNowPlaying == null ||
                             (currentShId.HasValue && previousNowPlaying.AzuraCastSongHistoryId != currentShId.Value);

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
                var playedAtTimestamp = nowPlayingData.NowPlaying.PlayedAt ?? 0;
                var playedAt = DateTimeOffset.FromUnixTimeSeconds(playedAtTimestamp).UtcDateTime;

                nowPlayingHistory = NowPlayingHistory.Create(
                    liveSessionId: session.Id,
                    songTitle: currentSong.Title ?? currentSong.Text ?? "Unknown",
                    songArtist: currentSong.Artist,
                    songAlbum: currentSong.Album,
                    songArtUrl: currentSong.Art,
                    durationSeconds: (int)(nowPlayingData.NowPlaying.Duration ?? 0),
                    playedAt: playedAt,
                    listenerCount: nowPlayingData.NowPlaying.Listeners ?? 0,
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
                var currentListeners = nowPlayingData.NowPlaying.Listeners ?? 0;
                nowPlayingHistory.UpdateListenerPeak(currentListeners);
                await _nowPlayingRepository.UpdateAsync(nowPlayingHistory, cancellationToken);
            }

            // 9. Update station sync status
            station.MarkSyncSuccessful(_dateTimeProvider);

            // 10. Map to Result using mapping extension
            var result = nowPlayingHistory.ToNowPlayingResult(session, _dateTimeProvider);

            return Result<NowPlayingResult>.Success(result);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed for session {SessionId}", command.SessionId);
            return Result<NowPlayingResult>.Failure(ex.Message, ex.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync now playing for session {SessionId}", command.SessionId);
            return Result<NowPlayingResult>.Failure(
                "An error occurred while syncing now playing data",
                500);
        }
    }
}
