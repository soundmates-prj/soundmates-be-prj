using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Constants;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Common.AzuraCast.Models;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Stations;
using LiveSessionService.Application.Services;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Stations.Commands.SyncStations;

/// <summary>
/// Handler for syncing stations from AzuraCast to local database with enhanced error handling
/// </summary>
public sealed class SyncStationsHandler : ICommandHandler<SyncStationsCommand, SyncStationsResult>
{
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IInputValidationService _validationService;
    private readonly ISyncConfigurationService _syncConfig;
    private readonly IAzuraCastErrorHandler _errorHandler;
    private readonly ILogger<SyncStationsHandler> _logger;

    public SyncStationsHandler(
        IAzuraCastClient azuraCastClient,
        IAzuraCastStationRepository stationRepository,
        IDateTimeProvider dateTimeProvider,
        IInputValidationService validationService,
        ISyncConfigurationService syncConfig,
        IAzuraCastErrorHandler errorHandler,
        ILogger<SyncStationsHandler> logger)
    {
        _azuraCastClient = azuraCastClient;
        _stationRepository = stationRepository;
        _dateTimeProvider = dateTimeProvider;
        _validationService = validationService;
        _syncConfig = syncConfig;
        _errorHandler = errorHandler;
        _logger = logger;
    }

    public async Task<Result<SyncStationsResult>> Handle(
        SyncStationsCommand command,
        CancellationToken cancellationToken)
    {
        var startTime = _dateTimeProvider.UtcNow;
        var attemptNumber = 0;
        Exception? lastException = null;

        while (attemptNumber < _syncConfig.MaxRetryAttempts)
        {
            attemptNumber++;
            
            try
            {
                _logger.LogInformation(
                    "Starting station sync from AzuraCast (Attempt {Attempt}/{Max})",
                    attemptNumber, _syncConfig.MaxRetryAttempts);

                // Fetch stations with timeout
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_syncConfig.SyncTimeout);

                var azuraCastStations = await _azuraCastClient.GetStationsAsync(cts.Token);

                if (azuraCastStations == null || azuraCastStations.Count == 0)
                {
                    _logger.LogWarning("No stations found in AzuraCast");
                    return Result<SyncStationsResult>.Failure(
                        "No stations found in AzuraCast. Please add stations in AzuraCast admin panel first.",
                        ErrorCode.NotFound);
                }

                // Validate data size
                if (azuraCastStations.Count > _syncConfig.MaxStationsPerSync)
                {
                    _logger.LogWarning(
                        "Too many stations from AzuraCast: {Count}. Max allowed: {Max}",
                        azuraCastStations.Count, _syncConfig.MaxStationsPerSync);
                    
                    return Result<SyncStationsResult>.Failure(
                        $"Too many stations ({azuraCastStations.Count}). Maximum allowed: {_syncConfig.MaxStationsPerSync}",
                        ErrorCode.BadRequest);
                }

                _logger.LogInformation("Found {Count} stations in AzuraCast", azuraCastStations.Count);

                var result = new SyncStationsResult
                {
                    TotalStations = azuraCastStations.Count,
                    CreatedStations = 0,
                    UpdatedStations = 0,
                    FailedStations = 0,
                    Errors = new List<string>(),
                    SyncedAt = _dateTimeProvider.UtcNow
                };

                // Process each station with validation
                foreach (var station in azuraCastStations)
                {
                    try
                    {
                        // Validate and sanitize input
                        var stationName = _validationService.SanitizeString(
                            station.Name, 100, "Unknown Station");
                        
                        var streamUrl = _validationService.SanitizeUrl(station.ListenUrl) 
                                       ?? "http://unknown";
                        
                        var description = _validationService.SanitizeString(
                            station.Description, 500, "");
                        
                        var shortcode = _validationService.SanitizeString(
                            station.Shortcode, 50, "");
                        
                        var publicPlayerUrl = _validationService.SanitizeUrl(station.PublicPlayerUrl);

                        var existing = await _stationRepository.GetByExternalIdAsync(
                            station.Id, cancellationToken);

                        if (existing == null)
                        {
                            // Create new station
                            var newStation = AzuraCastStation.Create(
                                externalStationId: station.Id,
                                stationName: stationName,
                                streamUrl: streamUrl,
                                description: description,
                                apiBaseUrl: null,
                                dateTimeProvider: _dateTimeProvider);

                            newStation.StationShortcode = shortcode;
                            newStation.PublicPlayerUrl = publicPlayerUrl;

                            await _stationRepository.AddAsync(newStation, cancellationToken);

                            // Sync mounts
                            if (station.Mounts?.Count > 0)
                            {
                                await SyncStationMountsAsync(
                                    newStation.Id, 
                                    station.Mounts, 
                                    cancellationToken);
                            }

                            result.CreatedStations++;
                            _logger.LogInformation(
                                "Created station: {StationName} (External ID: {ExternalId})",
                                stationName, station.Id);
                        }
                        else
                        {
                            // Update existing station if data changed
                            var hasChanges = false;

                            if (existing.StationName != stationName)
                            {
                                existing.UpdateStationName(stationName, _dateTimeProvider);
                                hasChanges = true;
                            }

                            if (existing.StreamUrl != streamUrl)
                            {
                                existing.UpdateStreamUrl(streamUrl, _dateTimeProvider);
                                hasChanges = true;
                            }

                            if (existing.Description != description)
                            {
                                existing.UpdateDescription(description, _dateTimeProvider);
                                hasChanges = true;
                            }

                            if (existing.StationShortcode != shortcode)
                            {
                                existing.StationShortcode = shortcode;
                                hasChanges = true;
                            }
                            
                            if (existing.PublicPlayerUrl != publicPlayerUrl)
                            {
                                existing.PublicPlayerUrl = publicPlayerUrl;
                                hasChanges = true;
                            }

                            // Mark sync successful
                            existing.MarkSyncSuccessful(_dateTimeProvider);

                            await _stationRepository.UpdateAsync(existing, cancellationToken);

                            if (hasChanges)
                            {
                                result.UpdatedStations++;
                                _logger.LogInformation(
                                    "Updated station: {StationName} (External ID: {ExternalId})",
                                    stationName, station.Id);
                            }

                            // Sync mounts
                            if (station.Mounts?.Count > 0)
                            {
                                await SyncStationMountsAsync(
                                    existing.Id, 
                                    station.Mounts, 
                                    cancellationToken);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedStations++;
                        var errorMsg = $"Station '{station.Name}' (ID: {station.Id}): {ex.Message}";
                        result.Errors.Add(errorMsg);
                        _logger.LogError(ex, "Failed to process station {StationId}", station.Id);
                    }
                }

                var duration = (int)(_dateTimeProvider.UtcNow - startTime).TotalSeconds;

                _logger.LogInformation(
                    "Sync completed: {Total} total, {Created} created, {Updated} updated, {Failed} failed, Duration={Duration}s",
                    result.TotalStations, result.CreatedStations, result.UpdatedStations, 
                    result.FailedStations, duration);

                return Result<SyncStationsResult>.Success(result);
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                // Check if we should retry
                if (_errorHandler.ShouldRetry(ex, attemptNumber))
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attemptNumber - 1) * 2); // Exponential backoff
                    _logger.LogWarning(
                        ex,
                        "Station sync attempt {Attempt} failed. Retrying in {Delay}s...",
                        attemptNumber, delay.TotalSeconds);
                    
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                // Don't retry, handle error
                break;
            }
        }

        // All retries failed, return error
        return HandleSyncError(lastException!);
    }

    private async Task SyncStationMountsAsync(
        Guid stationId,
        List<AzuraCastMountData> mounts,
        CancellationToken cancellationToken)
    {
        try
        {
            var now = _dateTimeProvider.UtcNow;
            var mountEntities = mounts.Select(m => new StationMount
            {
                Id = Guid.NewGuid(),
                AzuraCastStationId = stationId,
                ExternalMountId = m.Id,
                MountName = _validationService.SanitizeString(
                    m.Name ?? m.Path ?? "/stream", 100, "/stream"),
                MountPath = _validationService.SanitizeString(
                    m.Path ?? "/stream", 200, "/stream"),
                MountUrl = _validationService.SanitizeUrl(m.Url),
                IsDefault = m.IsDefault,
                Bitrate = m.Bitrate,
                Format = _validationService.SanitizeString(m.Format, 20, ""),
                CurrentListeners = m.Listeners?.Current,
                UniqueListeners = m.Listeners?.Unique,
                IsEnabled = true,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList();

            await _stationRepository.SyncMountsAsync(stationId, mountEntities, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync mounts for station {StationId}", stationId);
        }
    }

    private Result<SyncStationsResult> HandleSyncError(Exception exception)
    {
        var errorMessage = _errorHandler.GetUserFriendlyMessage(exception);
        var errorCode = ErrorCode.InternalServerError;

        if (exception is HttpRequestException httpEx)
        {
            errorCode = _errorHandler.MapHttpStatusToErrorCode(httpEx.StatusCode);
            _logger.LogError(httpEx, "HTTP error from AzuraCast: {StatusCode}", httpEx.StatusCode);
        }
        else if (exception is OperationCanceledException)
        {
            errorCode = ErrorCode.RequestTimeout;
            _logger.LogWarning("Station sync operation timed out");
        }
        else if (exception is DomainException domainEx)
        {
            errorCode = (ErrorCode)domainEx.StatusCode;
            _logger.LogWarning(domainEx, "Domain validation failed during station sync");
        }
        else
        {
            _logger.LogError(exception, "Unexpected error during station sync");
        }

        return Result<SyncStationsResult>.Failure(errorMessage, errorCode);
    }
}
