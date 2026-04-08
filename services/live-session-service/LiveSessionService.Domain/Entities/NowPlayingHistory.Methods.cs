using LiveSessionService.Domain.Errors;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Domain.Entities;

/// <summary>
/// Domain behavior methods for NowPlayingHistory entity
/// </summary>
public partial class NowPlayingHistory
{
    /// <summary>
    /// Factory method to create new now playing history entry
    /// 
    /// BUSINESS RULES:
    /// - Song title cannot be empty
    /// - Duration must be positive
    /// - Listener count must be non-negative
    /// </summary>
    public static NowPlayingHistory Create(
        Guid liveSessionId,
        string songTitle,
        string? songArtist,
        string? songAlbum,
        string? songArtUrl,
        string? lyrics,
        int durationSeconds,
        DateTime playedAt,
        int listenerCount,
        long? azuraCastSongHistoryId,
        bool isRequest,
        Guid? requestedByUserId,
        IDateTimeProvider dateTimeProvider)
    {
        // Validate session ID
        if (liveSessionId == Guid.Empty)
            throw new NowPlayingValidationException(
                "Session ID cannot be empty",
                NowPlayingErrorCodes.SessionIdEmpty);

        // Validate song title
        if (string.IsNullOrWhiteSpace(songTitle))
            throw new NowPlayingValidationException(
                "Song title cannot be empty",
                NowPlayingErrorCodes.SongTitleEmpty);

        // Validate duration
        if (durationSeconds < 0)
            throw new NowPlayingValidationException(
                "Duration must be non-negative",
                NowPlayingErrorCodes.InvalidDuration);

        // Validate listener count
        if (listenerCount < 0)
            throw new NowPlayingValidationException(
                "Listener count must be non-negative",
                NowPlayingErrorCodes.InvalidListenerCount);

        return new NowPlayingHistory
        {
            Id = Guid.NewGuid(),
            LiveSessionId = liveSessionId,
            AzuraCastSongHistoryId = azuraCastSongHistoryId,
            SongTitle = songTitle.Trim(),
            SongArtist = songArtist?.Trim(),
            SongAlbum = songAlbum?.Trim(),
            SongArtUrl = songArtUrl?.Trim(),
            Lyrics = lyrics,
            DurationSeconds = durationSeconds,
            PlayedAt = playedAt,
            ListenerCount = listenerCount,
            ListenerPeak = listenerCount, // Initially same as current count
            IsRequest = isRequest,
            RequestedByUserId = requestedByUserId,
            CreatedAt = dateTimeProvider.UtcNow
        };
    }

    /// <summary>
    /// Updates listener peak if current count is higher
    /// </summary>
    public void UpdateListenerPeak(int currentListenerCount)
    {
        if (currentListenerCount < 0)
            throw new NowPlayingValidationException(
                "Listener count must be non-negative",
                NowPlayingErrorCodes.InvalidListenerCount);

        if (currentListenerCount > ListenerPeak)
        {
            ListenerPeak = currentListenerCount;
        }

        ListenerCount = currentListenerCount;
    }

    /// <summary>
    /// Marks the song as ended
    /// </summary>
    public void MarkEnded(IDateTimeProvider dateTimeProvider)
    {
        if (EndedAt.HasValue)
            return; // Already ended

        EndedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Checks if this song is currently playing
    /// </summary>
    public bool IsCurrentlyPlaying() => !EndedAt.HasValue;
}
