namespace LiveSessionService.Domain.Errors;

/// <summary>
/// Error codes for LiveSession-related operations
/// Centralized constants for validation, state, and business rule errors
/// </summary>
public static class LiveSessionErrorCodes
{
    // Validation errors (400 Bad Request)
    public const string SessionNameEmpty = "SESSION_NAME_EMPTY";
    public const string SessionNameTooShort = "SESSION_NAME_TOO_SHORT";
    public const string SessionNameTooLong = "SESSION_NAME_TOO_LONG";
    public const string DescriptionTooLong = "SESSION_DESCRIPTION_TOO_LONG";
    public const string InvalidMaxListeners = "SESSION_INVALID_MAX_LISTENERS";
    public const string HostUserIdEmpty = "SESSION_HOST_USER_ID_EMPTY";
    public const string StationRequired = "SESSION_STATION_REQUIRED";

    // State transition errors (409 Conflict)
    public const string SessionAlreadyStarted = "SESSION_ALREADY_STARTED";
    public const string SessionAlreadyEnded = "SESSION_ALREADY_ENDED";
    public const string SessionNotActive = "SESSION_NOT_ACTIVE";
    public const string CannotEndNotStartedSession = "SESSION_CANNOT_END_NOT_STARTED";

    // Not found errors (404)
    public const string SessionNotFound = "SESSION_NOT_FOUND";

    // Authorization errors (403)
    public const string NotSessionHost = "SESSION_NOT_HOST";
    public const string SessionIsFull = "SESSION_IS_FULL";
    public const string SessionIsPrivate = "SESSION_IS_PRIVATE";
}
