using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Stations;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Stations.Commands.CreateStation;

/// <summary>
/// Handler for creating a new station and pushing to AzuraCast
/// 
/// FLOW:
/// 1. Create station in local DB
/// 2. Push station to AzuraCast via API
/// 3. Update local station with AzuraCast external ID
/// </summary>
public sealed class CreateStationHandler : ICommandHandler<CreateStationCommand, StationResult>
{
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CreateStationHandler> _logger;

    public CreateStationHandler(
        IAzuraCastStationRepository stationRepository,
        IAzuraCastClient azuraCastClient,
        IDateTimeProvider dateTimeProvider,
        ILogger<CreateStationHandler> logger)
    {
        _stationRepository = stationRepository;
        _azuraCastClient = azuraCastClient;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<StationResult>> Handle(
        CreateStationCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating station: {StationName}", command.StationName);

            // Create station in AzuraCast via API
            var azuraStation = await _azuraCastClient.CreateStationAsync(
                command.StationName,
                command.ShortCode,
                command.Description,
                command.Port,
                cancellationToken);

            if (azuraStation == null)
            {
                return Result<StationResult>.Failure(
                    "Failed to create station in AzuraCast API. No data returned.",
                    ErrorCode.InternalServerError);
            }

            var station = AzuraCastStation.Create(
                externalStationId: azuraStation.Id,
                stationName: azuraStation.Name ?? command.StationName,
                streamUrl: azuraStation.ListenUrl ?? "http://placeholder",
                description: command.Description,
                apiBaseUrl: null,
                dateTimeProvider: _dateTimeProvider);

            if (!string.IsNullOrWhiteSpace(azuraStation.Shortcode))
            {
                station.StationShortcode = azuraStation.Shortcode;
            }
            else if (!string.IsNullOrWhiteSpace(command.ShortCode))
            {
                station.StationShortcode = command.ShortCode.Trim();
            }

            if (!string.IsNullOrWhiteSpace(azuraStation.PublicPlayerUrl))
            {
                station.PublicPlayerUrl = azuraStation.PublicPlayerUrl;
            }

            await _stationRepository.AddAsync(station, cancellationToken);

            // Mark as Synced since we successfully created it in AzuraCast
            station.MarkSyncSuccessful(_dateTimeProvider);
            await _stationRepository.UpdateAsync(station, cancellationToken);

            _logger.LogInformation("Created and marked station {StationId} as Synced", station.Id);

            var result = new StationResult
            {
                Id = station.Id,
                ExternalStationId = station.ExternalStationId,
                StationName = station.StationName,
                StationShortcode = station.StationShortcode,
                StreamUrl = station.StreamUrl,
                Description = station.Description,
                IsEnabled = station.IsEnabled,
                SyncStatus = station.SyncStatus.ToString()
            };

            return Result<StationResult>.Success(result);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed");
            return Result<StationResult>.Failure(ex.Message, (ErrorCode)ex.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create station");
            return Result<StationResult>.Failure(
                "Failed to create station",
                ErrorCode.InternalServerError);
        }
    }
}
