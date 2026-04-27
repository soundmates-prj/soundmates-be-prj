using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using Microsoft.Extensions.Logging;

using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Stations.Commands.RestartStation;

public sealed class RestartStationHandler : ICommandHandler<RestartStationCommand, bool>
{
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly ILogger<RestartStationHandler> _logger;

    public RestartStationHandler(
        IAzuraCastStationRepository stationRepository,
        IAzuraCastClient azuraCastClient,
        ILogger<RestartStationHandler> logger)
    {
        _stationRepository = stationRepository;
        _azuraCastClient = azuraCastClient;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(RestartStationCommand command, CancellationToken cancellationToken)
    {
        var station = await _stationRepository.GetByIdAsync(command.StationId, cancellationToken);
        if (station == null)
            return Result<bool>.Failure("Station not found", ErrorCode.NotFound);

        _logger.LogInformation("Requesting Restart Station for AzuraCast station {StationId} (external: {ExternalId})", command.StationId, station.ExternalStationId);

        try
        {
            await _azuraCastClient.RestartStationAsync(station.ExternalStationId, cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart station {StationId}", station.ExternalStationId);
            return Result<bool>.Failure($"Failed to restart station: {ex.Message}", ErrorCode.InternalServerError);
        }
    }
}
