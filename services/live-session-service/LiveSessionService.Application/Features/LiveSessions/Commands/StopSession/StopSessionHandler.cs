using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.StopSession;

public sealed class StopSessionHandler : ICommandHandler<StopSessionCommand, LiveSessionResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<StopSessionHandler> _logger;
    private readonly ILiveSessionNotifier _notifier;

    public StopSessionHandler(
        ILiveSessionRepository sessionRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<StopSessionHandler> logger,
        ILiveSessionNotifier notifier)
    {
        _sessionRepository = sessionRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _notifier = notifier;
    }

    public async Task<Result<LiveSessionResult>> Handle(
        StopSessionCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessionRepository.GetByIdWithStationAsync(command.SessionId, cancellationToken);
            if (session == null)
            {
                return Result<LiveSessionResult>.Failure("Session not found", ErrorCode.NotFound);
            }

            session.Stop(_dateTimeProvider);
            await _sessionRepository.UpdateAsync(session, cancellationToken);
            await _sessionRepository.EndSessionCleanupAsync(
                session.Id,
                session.EndedAt ?? _dateTimeProvider.UtcNow,
                cancellationToken);

            _logger.LogInformation("Stopped session {SessionId}", command.SessionId);

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
                TotalDuration = session.EndedAt.HasValue
                    ? (int)(session.EndedAt.Value - session.StartedAt).TotalMinutes
                    : 0,
                StreamUrl = session.AzuraCastStation?.StreamUrl,
                ThumbnailUrl = session.ThumbnailUrl,
                Genre = session.Genre
            };

            await _notifier.NotifySessionEnded(result, cancellationToken);

            return Result<LiveSessionResult>.Success(result);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Failed to stop session");
            return Result<LiveSessionResult>.Failure(ex.Message, (ErrorCode)ex.StatusCode);
        }
    }
}
