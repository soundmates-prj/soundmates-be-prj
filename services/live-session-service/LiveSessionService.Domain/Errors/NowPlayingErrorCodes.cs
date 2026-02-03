namespace LiveSessionService.Domain.Errors;

/// <summary>
/// Error codes for NowPlaying-related operations
/// </summary>
public static class NowPlayingErrorCodes
{
    // Validation errors (400)
    public const string SongTitleEmpty = "NOWPLAYING_SONG_TITLE_EMPTY";
    public const string InvalidDuration = "NOWPLAYING_INVALID_DURATION";
    public const string InvalidListenerCount = "NOWPLAYING_INVALID_LISTENER_COUNT";
    public const string SessionIdEmpty = "NOWPLAYING_SESSION_ID_EMPTY";

    // Business rule errors (409)
    public const string SessionNotActive = "NOWPLAYING_SESSION_NOT_ACTIVE";
    public const string StationNotSynced = "NOWPLAYING_STATION_NOT_SYNCED";

    // Sync errors (500)
    public const string SyncFailed = "NOWPLAYING_SYNC_FAILED";
    public const string AzuraCastApiError = "NOWPLAYING_AZURACAST_API_ERROR";
}
