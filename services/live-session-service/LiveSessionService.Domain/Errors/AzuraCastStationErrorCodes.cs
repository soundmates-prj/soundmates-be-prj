namespace LiveSessionService.Domain.Errors;

/// <summary>
/// Error codes for AzuraCast station-related operations
/// </summary>
public static class AzuraCastStationErrorCodes
{
    // Validation errors (400)
    public const string StationNameEmpty = "STATION_NAME_EMPTY";
    public const string StationNameTooLong = "STATION_NAME_TOO_LONG";
    public const string StreamUrlEmpty = "STATION_STREAM_URL_EMPTY";
    public const string StreamUrlInvalid = "STATION_STREAM_URL_INVALID";
    public const string ApiBaseUrlEmpty = "STATION_API_BASE_URL_EMPTY";
    public const string ApiBaseUrlInvalid = "STATION_API_BASE_URL_INVALID";
    public const string ExternalStationIdInvalid = "STATION_EXTERNAL_ID_INVALID";

    // State errors (409)
    public const string StationAlreadyEnabled = "STATION_ALREADY_ENABLED";
    public const string StationAlreadyDisabled = "STATION_ALREADY_DISABLED";
    public const string StationInUse = "STATION_IN_USE";

    // Not found errors (404)
    public const string StationNotFound = "STATION_NOT_FOUND";

    // Sync errors (500)
    public const string SyncFailed = "STATION_SYNC_FAILED";
    public const string ApiConnectionFailed = "STATION_API_CONNECTION_FAILED";
}
