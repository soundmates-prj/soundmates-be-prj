using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Stations;
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
    private readonly ILogger<SyncStationsHandler> _logger;

    public SyncStationsHandler(
        IAzuraCastClient azuraCastClient,
        IAzuraCastStationRepository stationRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<SyncStationsHandler> logger)
    {
        _azuraCastClient = azuraCastClient;
        _stationRepository = stationRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<SyncStationsResult>> Handle(
        SyncStationsCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting station sync from AzuraCast");

            // Fetch stations with error handling
            var azuraCastStations = await _azuraCastClient.GetStationsAsync(cancellationToken);

            if (azuraCastStations == null || azuraCastStations.Count == 0)
            {
                _logger.LogWarning("No stations found in AzuraCast");
                return Result<SyncStationsResult>.Failure(
                    "No stations found in AzuraCast. Please add stations in AzuraCast admin panel first.",
                    ErrorCode.NotFound);
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

            // Process each station
            foreach (var station in azuraCastStations)
            {
                try
                {
                    var existing = await _stationRepository.GetByExternalIdAsync(station.Id, cancellationToken);

                    if (existing == null)
                    {
                        // Create new station
                        var newStation = AzuraCastStation.Create(
                            externalStationId: station.Id,
                            stationName: station.Name,
                            streamUrl: station.ListenUrl ?? "http://unknown",
                            description: station.Description,
                            apiBaseUrl: null,
                            dateTimeProvider: _dateTimeProvider);

                        await _stationRepository.AddAsync(newStation, cancellationToken);
                        result.CreatedStations++;
                        
                        _logger.LogInformation(
                            "Created station: {StationName} (External ID: {ExternalId})",
                            station.Name, station.Id);
                    }
                    else
                    {
                        // Update existing station if data changed
                        var hasChanges = false;

                        // Check and update station name
                        if (existing.StationName != station.Name)
                        {
                            existing.UpdateStationName(station.Name, _dateTimeProvider);
                            hasChanges = true;
                        }

                        // Check and update stream URL
                        var newStreamUrl = station.ListenUrl ?? existing.StreamUrl;
                        if (existing.StreamUrl != newStreamUrl)
                        {
                            existing.UpdateStreamUrl(newStreamUrl, _dateTimeProvider);
                            hasChanges = true;
                        }

                        // Check and update description
                        if (existing.Description != station.Description)
                        {
                            existing.UpdateDescription(station.Description, _dateTimeProvider);
                            hasChanges = true;
                        }

                        // Mark sync successful
                        existing.MarkSyncSuccessful(_dateTimeProvider);

                        if (hasChanges)
                        {
                            await _stationRepository.UpdateAsync(existing, cancellationToken);
                            result.UpdatedStations++;
                            
                            _logger.LogInformation(
                                "Updated station: {StationName} (External ID: {ExternalId})",
                                station.Name, station.Id);
                        }
                        else
                        {
                            // No changes, just update sync timestamp
                            await _stationRepository.UpdateAsync(existing, cancellationToken);
                            
                            _logger.LogDebug(
                                "No changes for station: {StationName} (External ID: {ExternalId})",
                                station.Name, station.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.FailedStations++;
                    result.Errors.Add($"Station '{station.Name}' (ID: {station.Id}): {ex.Message}");
                    _logger.LogError(ex, "Failed to process station {StationId}", station.Id);
                }
            }

            _logger.LogInformation(
                "Sync completed: {Total} total, {Created} created, {Updated} updated, {Failed} failed",
                result.TotalStations, result.CreatedStations, result.UpdatedStations, result.FailedStations);

            return Result<SyncStationsResult>.Success(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error from AzuraCast");
            
            var errorMessage = ex.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized =>
                    "Authentication failed with AzuraCast. Please verify your API key configuration.",
                System.Net.HttpStatusCode.ServiceUnavailable =>
                    "AzuraCast service is unavailable. Please ensure it is running.",
                _ => $"Failed to connect to AzuraCast: {ex.Message}"
            };

            var errorCode = ex.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? ErrorCode.Unauthorized
                : ErrorCode.ServiceUnavailable;

            return Result<SyncStationsResult>.Failure(errorMessage, errorCode);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed");
            return Result<SyncStationsResult>.Failure(ex.Message, (ErrorCode)ex.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during sync");
            return Result<SyncStationsResult>.Failure(
                $"An unexpected error occurred: {ex.Message}",
                ErrorCode.InternalServerError);
        }
    }
}
