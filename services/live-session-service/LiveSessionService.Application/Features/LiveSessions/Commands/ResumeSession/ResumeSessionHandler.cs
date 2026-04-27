using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.ResumeSession;

public sealed class ResumeSessionHandler : ICommandHandler<ResumeSessionCommand, LiveSessionResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ResumeSessionHandler> _logger;

    public ResumeSessionHandler(
        ILiveSessionRepository sessionRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<ResumeSessionHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<LiveSessionResult>> Handle(
        ResumeSessionCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessionRepository.GetByIdWithStationAsync(command.SessionId, cancellationToken);
            if (session == null)
            {
                return Result<LiveSessionResult>.Failure("Session not found", ErrorCode.NotFound);
            }

            session.Resume(_dateTimeProvider);
            await _sessionRepository.UpdateAsync(session, cancellationToken);

            _logger.LogInformation("Resumed session {SessionId}", command.SessionId);

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
                CreatedAt = session.CreatedAt
            };

            return Result<LiveSessionResult>.Success(result);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Failed to resume session");
            return Result<LiveSessionResult>.Failure(ex.Message, (ErrorCode)ex.StatusCode);
        }
    }
}
