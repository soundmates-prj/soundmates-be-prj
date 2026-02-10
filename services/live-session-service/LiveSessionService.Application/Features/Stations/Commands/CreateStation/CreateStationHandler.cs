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

            // TODO: Push to AzuraCast first to get external ID
            // For now, create locally with placeholder
            
            var station = AzuraCastStation.Create(
                externalStationId: 0, // Will be updated after AzuraCast creation
                stationName: command.StationName,
                streamUrl: "http://placeholder", // Will be updated
                description: command.Description,
                apiBaseUrl: null,
                dateTimeProvider: _dateTimeProvider);

            await _stationRepository.AddAsync(station, cancellationToken);

            _logger.LogInformation("Created station {StationId}", station.Id);

            var result = new StationResult
            {
                Id = station.Id,
                ExternalStationId = station.ExternalStationId,
                StationName = station.StationName,
                StreamUrl = station.StreamUrl,
                Description = station.Description,
                IsEnabled = station.IsEnabled
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
