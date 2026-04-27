namespace LiveSessionService.Domain.Enums;

public enum ActivityType
{
    SessionStarted = 0,
    SessionEnded = 1,
    SessionPaused = 2,
    SessionResumed = 3,
    UserJoined = 4,
    UserLeft = 5,
    SongChanged = 6,
    PlaylistUpdated = 7,
    ListenerPeakReached = 8,
    ModeratorAction = 9,
    ChatMessage = 10,
    SongRequested = 11,
    Error = 99
}
