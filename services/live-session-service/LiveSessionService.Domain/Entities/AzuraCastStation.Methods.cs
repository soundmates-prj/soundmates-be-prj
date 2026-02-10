using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Errors;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Domain.ValueObjects;

namespace LiveSessionService.Domain.Entities;

/// <summary>
/// Domain behavior methods for AzuraCastStation entity
/// Rich Domain Model following Clean Architecture principles
/// </summary>
public partial class AzuraCastStation
{
    private const int MaxStationNameLength = 100;
    private const int MaxDescriptionLength = 500;

    /// <summary>
    /// Factory method to create a new AzuraCast station
    /// 
    /// BUSINESS RULES:
    /// - Station name cannot be empty
    /// - Stream URL must be valid HTTP/HTTPS
    /// - API base URL must be valid HTTP/HTTPS
    /// - External station ID must be positive
    /// </summary>
    public static AzuraCastStation Create(
        int externalStationId,
        string stationName,
        string streamUrl,
        string? apiBaseUrl,
        string? description,
        IDateTimeProvider dateTimeProvider)
    {
        // Validate external station ID
        if (externalStationId <= 0)
            throw new AzuraCastStationValidationException(
                "External station ID must be positive",
                AzuraCastStationErrorCodes.ExternalStationIdInvalid);

        // Validate station name
        if (string.IsNullOrWhiteSpace(stationName))
            throw new AzuraCastStationValidationException(
                "Station name cannot be empty",
                AzuraCastStationErrorCodes.StationNameEmpty);

        var trimmedName = stationName.Trim();
        if (trimmedName.Length > MaxStationNameLength)
            throw new AzuraCastStationValidationException(
                $"Station name cannot exceed {MaxStationNameLength} characters",
                AzuraCastStationErrorCodes.StationNameTooLong);

        // Validate description
        if (description != null && description.Length > MaxDescriptionLength)
            throw new AzuraCastStationValidationException(
                $"Description cannot exceed {MaxDescriptionLength} characters",
                AzuraCastStationErrorCodes.StationNameTooLong);

        // Validate stream URL
        if (string.IsNullOrWhiteSpace(streamUrl))
            throw new AzuraCastStationValidationException(
                "Stream URL cannot be empty",
                AzuraCastStationErrorCodes.StreamUrlEmpty);

        // Validate API base URL if provided
        if (!string.IsNullOrWhiteSpace(apiBaseUrl) && !apiBaseUrl.StartsWith("http"))
            throw new AzuraCastStationValidationException(
                "API base URL must start with http:// or https://",
                AzuraCastStationErrorCodes.ApiBaseUrlInvalid);

        var now = dateTimeProvider.UtcNow;

        return new AzuraCastStation
        {
            Id = Guid.NewGuid(),
            ExternalStationId = externalStationId,
            StationName = trimmedName,
            Description = description?.Trim(),
            StreamUrl = streamUrl.Trim(),
            ApiBaseUrl = apiBaseUrl?.Trim().TrimEnd('/'),
            IsEnabled = true,
            SyncStatus = StationSyncStatus.NotSynced,
            CreatedAt = now
        };
    }

    /// <summary>
    /// Enables the station
    /// </summary>
    public void Enable(IDateTimeProvider dateTimeProvider)
    {
        if (IsEnabled)
            throw new InvalidStationStateException(
                "Station is already enabled",
                AzuraCastStationErrorCodes.StationAlreadyEnabled);

        IsEnabled = true;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Disables the station
    /// </summary>
    public void Disable(IDateTimeProvider dateTimeProvider)
    {
        if (!IsEnabled)
            throw new InvalidStationStateException(
                "Station is already disabled",
                AzuraCastStationErrorCodes.StationAlreadyDisabled);

        IsEnabled = false;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Updates sync status after successful sync
    /// </summary>
    public void MarkSyncSuccessful(IDateTimeProvider dateTimeProvider)
    {
        SyncStatus = StationSyncStatus.Synced;
        LastSyncedAt = dateTimeProvider.UtcNow;
        LastSyncError = null;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Updates sync status after failed sync
    /// </summary>
    public void MarkSyncFailed(string error, IDateTimeProvider dateTimeProvider)
    {
        SyncStatus = StationSyncStatus.Failed;
        LastSyncError = error;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Marks station as currently syncing
    /// </summary>
    public void MarkSyncing(IDateTimeProvider dateTimeProvider)
    {
        SyncStatus = StationSyncStatus.Syncing;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Updates station details
    /// </summary>
    public void UpdateDetails(
        string stationName,
        string? description,
        string streamUrl,
        string? apiBaseUrl,
        IDateTimeProvider dateTimeProvider)
    {
        // Validate station name
        if (string.IsNullOrWhiteSpace(stationName))
            throw new AzuraCastStationValidationException(
                "Station name cannot be empty",
                AzuraCastStationErrorCodes.StationNameEmpty);

        var trimmedName = stationName.Trim();
        if (trimmedName.Length > MaxStationNameLength)
            throw new AzuraCastStationValidationException(
                $"Station name cannot exceed {MaxStationNameLength} characters",
                AzuraCastStationErrorCodes.StationNameTooLong);

        // Validate stream URL
        if (string.IsNullOrWhiteSpace(streamUrl))
            throw new AzuraCastStationValidationException(
                "Stream URL cannot be empty",
                AzuraCastStationErrorCodes.StreamUrlEmpty);

        // Validate API base URL if provided
        if (!string.IsNullOrWhiteSpace(apiBaseUrl) && !apiBaseUrl.StartsWith("http"))
            throw new AzuraCastStationValidationException(
                "API base URL must start with http:// or https://",
                AzuraCastStationErrorCodes.ApiBaseUrlInvalid);

        StationName = trimmedName;
        Description = description?.Trim();
        StreamUrl = streamUrl.Trim();
        ApiBaseUrl = apiBaseUrl?.Trim().TrimEnd('/');
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Updates station name only
    /// Used during sync when only name changed
    /// </summary>
    public void UpdateStationName(string stationName, IDateTimeProvider dateTimeProvider)
    {
        if (string.IsNullOrWhiteSpace(stationName))
            throw new AzuraCastStationValidationException(
                "Station name cannot be empty",
                AzuraCastStationErrorCodes.StationNameEmpty);

        var trimmedName = stationName.Trim();
        if (trimmedName.Length > MaxStationNameLength)
            throw new AzuraCastStationValidationException(
                $"Station name cannot exceed {MaxStationNameLength} characters",
                AzuraCastStationErrorCodes.StationNameTooLong);

        StationName = trimmedName;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Updates stream URL only
    /// Used during sync when only URL changed
    /// </summary>
    public void UpdateStreamUrl(string streamUrl, IDateTimeProvider dateTimeProvider)
    {
        if (string.IsNullOrWhiteSpace(streamUrl))
            throw new AzuraCastStationValidationException(
                "Stream URL cannot be empty",
                AzuraCastStationErrorCodes.StreamUrlEmpty);

        StreamUrl = streamUrl.Trim();
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    /// <summary>
    /// Updates description only
    /// Used during sync when only description changed
    /// </summary>
    public void UpdateDescription(string? description, IDateTimeProvider dateTimeProvider)
    {
        if (description != null && description.Length > MaxDescriptionLength)
            throw new AzuraCastStationValidationException(
                $"Description cannot exceed {MaxDescriptionLength} characters",
                AzuraCastStationErrorCodes.StationNameTooLong);

        Description = description?.Trim();
        UpdatedAt = dateTimeProvider.UtcNow;
    }
}
