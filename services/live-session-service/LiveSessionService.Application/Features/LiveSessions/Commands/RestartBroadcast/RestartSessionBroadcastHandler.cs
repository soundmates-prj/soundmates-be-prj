using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.RestartBroadcast;

public sealed class RestartSessionBroadcastHandler : ICommandHandler<RestartSessionBroadcastCommand, bool>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly ILogger<RestartSessionBroadcastHandler> _logger;

    public RestartSessionBroadcastHandler(
        ILiveSessionRepository sessionRepository,
        IAzuraCastClient azuraCastClient,
        ILogger<RestartSessionBroadcastHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _azuraCastClient = azuraCastClient;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(RestartSessionBroadcastCommand command, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdWithStationAsync(command.SessionId, cancellationToken);
        if (session == null)
            return Result<bool>.Failure("Session not found", ErrorCode.NotFound);

        if (session.AzuraCastStation == null)
            return Result<bool>.Failure("Session does not have an associated station", ErrorCode.NotFound);

        _logger.LogInformation(
            "Requesting Restart Station from AzuraCast for session {SessionId} (external station: {ExternalId})",
            command.SessionId, session.AzuraCastStation.ExternalStationId);

        try
        {
            await _azuraCastClient.RestartStationAsync(session.AzuraCastStation.ExternalStationId, cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (LiveSessionService.Application.Exceptions.AzuraCastException ex)
        {
            _logger.LogError(ex, "Failed to restart station {StationId} (AzuraCast API error)", session.AzuraCastStation.ExternalStationId);
            return Result<bool>.Failure($"Failed to restart station: {ex.Message}", ex.ErrorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart station {StationId} (Internal error)", session.AzuraCastStation.ExternalStationId);
            return Result<bool>.Failure($"Failed to restart station: {ex.Message}", ErrorCode.InternalServerError);
        }
    }
}
