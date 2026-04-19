using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.ReloadBroadcast;

public sealed class ReloadSessionBroadcastHandler : ICommandHandler<ReloadSessionBroadcastCommand, bool>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly ILogger<ReloadSessionBroadcastHandler> _logger;

    public ReloadSessionBroadcastHandler(
        ILiveSessionRepository sessionRepository,
        IAzuraCastClient azuraCastClient,
        ILogger<ReloadSessionBroadcastHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _azuraCastClient = azuraCastClient;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ReloadSessionBroadcastCommand command, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdWithStationAsync(command.SessionId, cancellationToken);
        if (session == null)
            return Result<bool>.Failure("Session not found", ErrorCode.NotFound);

        if (session.AzuraCastStation == null)
            return Result<bool>.Failure("Session does not have an associated station", ErrorCode.NotFound);

        _logger.LogInformation(
            "Requesting Reload Station from AzuraCast for session {SessionId} (external station: {ExternalId})",
            command.SessionId, session.AzuraCastStation.ExternalStationId);

        try
        {
            await _azuraCastClient.ReloadStationAsync(session.AzuraCastStation.ExternalStationId, cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload station {StationId}", session.AzuraCastStation.ExternalStationId);
            return Result<bool>.Failure($"Failed to reload station: {ex.Message}", ErrorCode.InternalServerError);
        }
    }
}
