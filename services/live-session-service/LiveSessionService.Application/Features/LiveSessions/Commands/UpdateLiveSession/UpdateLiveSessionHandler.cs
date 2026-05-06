using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.UpdateLiveSession;

public sealed class UpdateLiveSessionHandler : ICommandHandler<UpdateLiveSessionCommand, LiveSessionResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateLiveSessionHandler(
        ILiveSessionRepository sessionRepository,
        IAzuraCastStationRepository stationRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _sessionRepository = sessionRepository;
        _stationRepository = stationRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<LiveSessionResult>> Handle(
        UpdateLiveSessionCommand command,
        CancellationToken cancellationToken)
    {
        if (command.HostUserId is null
            && command.StationId is null
            && command.SessionName is null
            && command.Description is null
            && command.ThumbnailUrl is null)
        {
            return Result<LiveSessionResult>.Failure("No updates provided", ErrorCode.BadRequest);
        }

        if (command.HostUserId.HasValue && command.HostUserId.Value == Guid.Empty)
        {
            return Result<LiveSessionResult>.Failure("Host user ID cannot be empty", ErrorCode.BadRequest);
        }

        var session = await _sessionRepository.GetByIdWithStationAsync(command.SessionId, cancellationToken);
        if (session == null)
        {
            return Result<LiveSessionResult>.Failure("Session not found", ErrorCode.NotFound);
        }

        try
        {
            if (command.HostUserId.HasValue && command.HostUserId.Value != session.HostUserId)
            {
                session.HostUserId = command.HostUserId.Value;
            }

            if (command.StationId.HasValue && command.StationId.Value != session.AzuraCastStationId)
            {
                var station = await _stationRepository.GetByIdAsync(command.StationId.Value, cancellationToken);
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

                session.AzuraCastStationId = station.Id;
                session.AzuraCastStation = station;
            }

            if (command.ThumbnailUrl is not null)
            {
                session.ThumbnailUrl = string.IsNullOrWhiteSpace(command.ThumbnailUrl)
                    ? null
                    : command.ThumbnailUrl.Trim();
            }

            var sessionName = command.SessionName ?? session.SessionName;
            var description = command.Description is null
                ? session.Description
                : string.IsNullOrWhiteSpace(command.Description) ? null : command.Description;

            session.UpdateDetails(sessionName, description, session.Genre, _dateTimeProvider);
            session.UpdatedAt = _dateTimeProvider.UtcNow;

            await _sessionRepository.UpdateAsync(session, cancellationToken);

            return Result<LiveSessionResult>.Success(new LiveSessionResult
            {
                Id = session.Id,
                UserId = session.HostUserId,
                StationId = session.AzuraCastStationId!.Value,
                StationName = session.AzuraCastStation?.StationName,
                SessionName = session.SessionName,
                Description = session.Description,
                Status = session.Status.ToString(),
                ScheduledStartAt = session.Status == SessionStatus.Scheduled ? session.StartedAt : null,
                StartedAt = session.StartedAt,
                EndedAt = session.EndedAt,
                CreatedAt = session.CreatedAt,
                StreamUrl = session.AzuraCastStation?.StreamUrl,
                StationShortcode = session.AzuraCastStation?.StationShortcode,
                PublicPlayerUrl = session.AzuraCastStation?.PublicPlayerUrl,
                ThumbnailUrl = session.ThumbnailUrl,
                Genre = session.Genre
            });
        }
        catch (DomainException ex)
        {
            return Result<LiveSessionResult>.Failure(ex.Message, (ErrorCode)ex.StatusCode);
        }
    }
}
