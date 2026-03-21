using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.StartSession;

/// <summary>
/// Handler for starting a live session
/// </summary>
public sealed class StartSessionHandler : ICommandHandler<StartSessionCommand, LiveSessionResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<StartSessionHandler> _logger;
    private readonly ILiveSessionNotifier _notifier;

    public StartSessionHandler(
        ILiveSessionRepository sessionRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<StartSessionHandler> logger,
        ILiveSessionNotifier notifier)
    {
        _sessionRepository = sessionRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _notifier = notifier;
    }

    public async Task<Result<LiveSessionResult>> Handle(
        StartSessionCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessionRepository.GetByIdWithStationAsync(command.SessionId, cancellationToken);
            
            if (session == null)
            {
                return Result<LiveSessionResult>.Failure(
                    "Session not found",
                    ErrorCode.NotFound);
            }

            session.Start(_dateTimeProvider);
            await _sessionRepository.UpdateAsync(session, cancellationToken);

            _logger.LogInformation("Started session {SessionId}", command.SessionId);

            var result = new LiveSessionResult
            {
                Id = session.Id,
                UserId = session.HostUserId,
                StationId = session.AzuraCastStationId!.Value,
                StationName = session.AzuraCastStation?.StationName,
                SessionName = session.SessionName,
                Description = session.Description,
                Status = session.Status.ToString(),
                StartedAt = session.StartedAt,
                EndedAt = session.EndedAt,
                CreatedAt = session.CreatedAt,
                StreamUrl = session.AzuraCastStation?.StreamUrl,
                ThumbnailUrl = session.ThumbnailUrl,
                Genre = session.Genre
            };

            await _notifier.NotifySessionStarted(result, cancellationToken);

            return Result<LiveSessionResult>.Success(result);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Failed to start session");
            return Result<LiveSessionResult>.Failure(ex.Message, (ErrorCode)ex.StatusCode);
        }
    }
}
