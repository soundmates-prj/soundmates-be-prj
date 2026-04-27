using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.CreateLiveSession;

public sealed class CreateLiveSessionHandler : ICommandHandler<CreateLiveSessionCommand, LiveSessionResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CreateLiveSessionHandler> _logger;

    public CreateLiveSessionHandler(
        ILiveSessionRepository sessionRepository,
        IAzuraCastStationRepository stationRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<CreateLiveSessionHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _stationRepository = stationRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<LiveSessionResult>> Handle(
        CreateLiveSessionCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Creating live session for user {UserId} with station {StationId}",
                command.UserId, command.StationId);

            // 1. Validate station exists and is enabled
            var station = await _stationRepository.GetByIdAsync(command.StationId, cancellationToken);
            if (station == null)
            {
                return Result<LiveSessionResult>.Failure("Station not found", ErrorCode.NotFound);
            }

            if (!station.IsEnabled)
            {
                return Result<LiveSessionResult>.Failure(
                    "Station is disabled and cannot be used for live sessions",
                    ErrorCode.BadRequest);
            }

            // 2. Create live session
            var session = LiveSession.Create(
                hostUserId: command.UserId,
                sessionName: command.SessionName,
                description: command.Description,
                azuraCastStationId: command.StationId,
                maxListeners: 100,
                isPublic: true,
                genre: null,
                scheduledStartAt: null,
                dateTimeProvider: _dateTimeProvider);

            // 3. Save to database
            await _sessionRepository.AddAsync(session, cancellationToken);

            _logger.LogInformation(
                "Created live session {SessionId} for user {UserId} with status {Status}",
                session.Id, command.UserId, session.Status);

            // 4. Map to result
            var result = new LiveSessionResult
            {
                Id = session.Id,
                UserId = session.HostUserId,
                StationId = session.AzuraCastStationId!.Value,
                StationName = station.StationName,
                SessionName = session.SessionName,
                Description = session.Description,
                Status = session.Status.ToString(),
                ScheduledStartAt = null,
                StartedAt = session.Status is SessionStatus.Live or SessionStatus.Paused or SessionStatus.Ended
                    ? session.StartedAt
                    : null,
                EndedAt = session.EndedAt,
                TotalListeners = 0,
                PeakListeners = 0,
                TotalDuration = 0,
                CreatedAt = session.CreatedAt
            };

            return Result<LiveSessionResult>.Success(result);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed");
            return Result<LiveSessionResult>.Failure(ex.Message, (ErrorCode)ex.StatusCode);
        }
    }
}
