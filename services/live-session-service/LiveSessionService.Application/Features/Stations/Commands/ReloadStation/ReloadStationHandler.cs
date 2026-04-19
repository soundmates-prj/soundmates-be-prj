using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using Microsoft.Extensions.Logging;

using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.Stations.Commands.ReloadStation;

public sealed class ReloadStationHandler : ICommandHandler<ReloadStationCommand, bool>
{
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly ILogger<ReloadStationHandler> _logger;

    public ReloadStationHandler(
        IAzuraCastStationRepository stationRepository,
        IAzuraCastClient azuraCastClient,
        ILogger<ReloadStationHandler> logger)
    {
        _stationRepository = stationRepository;
        _azuraCastClient = azuraCastClient;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ReloadStationCommand command, CancellationToken cancellationToken)
    {
        var station = await _stationRepository.GetByIdAsync(command.StationId, cancellationToken);
        if (station == null)
            return Result<bool>.Failure("Station not found", ErrorCode.NotFound);

        _logger.LogInformation("Requesting Reload Station for AzuraCast station {StationId} (external: {ExternalId})", command.StationId, station.ExternalStationId);

        try
        {
            await _azuraCastClient.ReloadStationAsync(station.ExternalStationId, cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload station {StationId}", station.ExternalStationId);
            return Result<bool>.Failure($"Failed to reload station: {ex.Message}", ErrorCode.InternalServerError);
        }
    }
}
