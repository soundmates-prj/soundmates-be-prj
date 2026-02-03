using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Stations.Commands.SyncStations;

/// <summary>
/// Handler for syncing stations from AzuraCast to local database
/// 
/// RESPONSIBILITIES:
/// 1. Fetch all stations from AzuraCast API
/// 2. For each station:
///    - Check if exists in DB (by ExternalStationId)
///    - Create new or update existing station
/// 3. Return sync statistics
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

            // 1. Fetch all stations from AzuraCast
            var azuraCastStations = await _azuraCastClient.GetStationsAsync(cancellationToken);

            if (azuraCastStations == null || azuraCastStations.Count == 0)
            {
                _logger.LogWarning("No stations found in AzuraCast");
                return Result<SyncStationsResult>.Success(new SyncStationsResult
                {
                    TotalStations = 0,
                    CreatedStations = 0,
                    UpdatedStations = 0,
                    FailedStations = 0
                });
            }

            _logger.LogInformation("Found {Count} stations in AzuraCast", azuraCastStations.Count);

            var result = new SyncStationsResult
            {
                TotalStations = azuraCastStations.Count,
                CreatedStations = 0,
                UpdatedStations = 0,
                FailedStations = 0,
                Errors = new List<string>()
            };

            // 2. Process each station
            foreach (var azuraCastStation in azuraCastStations)
            {
                try
                {
                    // Check if station exists in DB
                    var existingStation = await _stationRepository.GetByExternalIdAsync(
                        azuraCastStation.Id,
                        cancellationToken);

                    if (existingStation == null)
                    {
                        // Create new station
                        // Use ListenUrl from API, or construct default if not provided
                        var streamUrl = azuraCastStation.ListenUrl ?? azuraCastStation.PublicPlayerUrl ?? "http://unknown";
                        var apiBaseUrl = !string.IsNullOrEmpty(azuraCastStation.PublicPlayerUrl) 
                            ? new Uri(azuraCastStation.PublicPlayerUrl).GetLeftPart(UriPartial.Authority)
                            : null;

                        var newStation = AzuraCastStation.Create(
                            externalStationId: azuraCastStation.Id,
                            stationName: azuraCastStation.Name,
                            streamUrl: streamUrl,
                            apiBaseUrl: apiBaseUrl,
                            description: azuraCastStation.Description,
                            dateTimeProvider: _dateTimeProvider);

                        // Set additional properties
                        newStation.StationShortcode = azuraCastStation.Shortcode;
                        newStation.PublicPlayerUrl = azuraCastStation.PublicPlayerUrl;

                        // Mark as synced
                        newStation.MarkSyncSuccessful(_dateTimeProvider);

                        await _stationRepository.AddAsync(newStation, cancellationToken);

                        result.CreatedStations++;

                        _logger.LogInformation(
                            "Created station: {StationName} (External ID: {ExternalId})",
                            newStation.StationName,
                            newStation.ExternalStationId);
                    }
                    else
                    {
                        // Update existing station
                        var streamUrl = azuraCastStation.ListenUrl ?? azuraCastStation.PublicPlayerUrl ?? existingStation.StreamUrl;
                        var apiBaseUrl = !string.IsNullOrEmpty(azuraCastStation.PublicPlayerUrl) 
                            ? new Uri(azuraCastStation.PublicPlayerUrl).GetLeftPart(UriPartial.Authority)
                            : existingStation.ApiBaseUrl;

                        existingStation.UpdateDetails(
                            stationName: azuraCastStation.Name,
                            description: azuraCastStation.Description,
                            streamUrl: streamUrl,
                            apiBaseUrl: apiBaseUrl,
                            dateTimeProvider: _dateTimeProvider);

                        // Update additional properties
                        existingStation.StationShortcode = azuraCastStation.Shortcode;
                        existingStation.PublicPlayerUrl = azuraCastStation.PublicPlayerUrl;

                        // Mark as synced
                        existingStation.MarkSyncSuccessful(_dateTimeProvider);

                        await _stationRepository.UpdateAsync(existingStation, cancellationToken);

                        result.UpdatedStations++;

                        _logger.LogInformation(
                            "Updated station: {StationName} (External ID: {ExternalId})",
                            existingStation.StationName,
                            existingStation.ExternalStationId);
                    }
                }
                catch (DomainException ex)
                {
                    result.FailedStations++;
                    result.Errors.Add($"Station {azuraCastStation.Name}: {ex.Message}");

                    _logger.LogWarning(ex,
                        "Failed to sync station {StationName} (ID: {StationId}): {Message}",
                        azuraCastStation.Name,
                        azuraCastStation.Id,
                        ex.Message);
                }
                catch (Exception ex)
                {
                    result.FailedStations++;
                    result.Errors.Add($"Station {azuraCastStation.Name}: Unexpected error");

                    _logger.LogError(ex,
                        "Unexpected error syncing station {StationName} (ID: {StationId})",
                        azuraCastStation.Name,
                        azuraCastStation.Id);
                }
            }

            _logger.LogInformation(
                "Station sync completed. Created: {Created}, Updated: {Updated}, Failed: {Failed}",
                result.CreatedStations,
                result.UpdatedStations,
                result.FailedStations);

            return Result<SyncStationsResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync stations from AzuraCast");
            return Result<SyncStationsResult>.Failure(
                "Failed to sync stations from AzuraCast",
                ErrorCode.InternalServerError);
        }
    }
}
