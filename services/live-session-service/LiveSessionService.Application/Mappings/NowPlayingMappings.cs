using LiveSessionService.Application.Features.Results.NowPlaying;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Mappings;

/// <summary>
/// Mapping extensions for NowPlaying entities
/// Maps Domain entities to Application Results
/// </summary>
public static class NowPlayingMappings
{
    /// <summary>
    /// Map Domain NowPlayingHistory entity to Application NowPlayingResult
    /// </summary>
    public static NowPlayingResult ToNowPlayingResult(
        this NowPlayingHistory nowPlaying,
        LiveSession session,
        IDateTimeProvider dateTimeProvider)
    {
        if (nowPlaying == null) throw new ArgumentNullException(nameof(nowPlaying));
        if (session == null) throw new ArgumentNullException(nameof(session));

        return new NowPlayingResult
        {
            Id = nowPlaying.Id,
            SessionId = session.Id,
            SessionName = session.SessionName,
            CurrentSong = new SongResult
            {
                Title = nowPlaying.SongTitle,
                Artist = nowPlaying.SongArtist,
                Album = nowPlaying.SongAlbum,
                ArtUrl = nowPlaying.SongArtUrl,
                DurationSeconds = nowPlaying.DurationSeconds,
                IsRequest = nowPlaying.IsRequest,
                RequestedByUserId = nowPlaying.RequestedByUserId
            },
            ListenerCount = nowPlaying.ListenerCount,
            ListenerPeak = nowPlaying.ListenerPeak,
            PlayedAt = nowPlaying.PlayedAt,
            EndedAt = nowPlaying.EndedAt,
            IsLive = nowPlaying.IsCurrentlyPlaying(),
            SyncedAt = dateTimeProvider.UtcNow
        };
    }

    /// <summary>
    /// Map Domain NowPlayingHistory entity to Application NowPlayingHistoryResult
    /// </summary>
    public static NowPlayingHistoryResult ToHistoryResult(this NowPlayingHistory nowPlaying)
    {
        if (nowPlaying == null) throw new ArgumentNullException(nameof(nowPlaying));

        return new NowPlayingHistoryResult
        {
            Id = nowPlaying.Id,
            Song = new SongResult
            {
                Title = nowPlaying.SongTitle,
                Artist = nowPlaying.SongArtist,
                Album = nowPlaying.SongAlbum,
                ArtUrl = nowPlaying.SongArtUrl,
                DurationSeconds = nowPlaying.DurationSeconds,
                IsRequest = nowPlaying.IsRequest,
                RequestedByUserId = nowPlaying.RequestedByUserId
            },
            PlayedAt = nowPlaying.PlayedAt,
            EndedAt = nowPlaying.EndedAt,
            ListenerPeak = nowPlaying.ListenerPeak,
            DurationSeconds = nowPlaying.DurationSeconds
        };
    }

    /// <summary>
    /// Map collection of Domain entities to Application results
    /// </summary>
    public static List<NowPlayingHistoryResult> ToHistoryResults(
        this IEnumerable<NowPlayingHistory> nowPlayingList)
    {
        if (nowPlayingList == null) throw new ArgumentNullException(nameof(nowPlayingList));

        return nowPlayingList.Select(np => np.ToHistoryResult()).ToList();
    }
}
