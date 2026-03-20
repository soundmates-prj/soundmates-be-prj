using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.CreateSessionSchedule;

public sealed class CreateSessionScheduleHandler : ICommandHandler<CreateSessionScheduleCommand, LiveSessionResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly ISessionScheduleRepository _scheduleRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateSessionScheduleHandler(
        ILiveSessionRepository sessionRepository,
        ISessionScheduleRepository scheduleRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _sessionRepository = sessionRepository;
        _scheduleRepository = scheduleRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<LiveSessionResult>> Handle(
        CreateSessionScheduleCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            if (command.EndTime <= command.StartTime)
            {
                return Result<LiveSessionResult>.Failure("End time must be greater than start time", ErrorCode.BadRequest);
            }

            var session = await _sessionRepository.GetByIdWithStationAsync(command.LiveSessionId, cancellationToken);
            if (session == null)
            {
                return Result<LiveSessionResult>.Failure("Session not found", ErrorCode.NotFound);
            }

            var schedule = new SessionSchedule
            {
                Id = Guid.NewGuid(),
                LiveSessionId = session.Id,
                StartTime = command.StartTime,
                EndTime = command.EndTime,
                Title = string.IsNullOrWhiteSpace(command.Title) ? session.SessionName : command.Title.Trim(),
                Status = "Scheduled"
            };

            await _scheduleRepository.AddAsync(schedule, cancellationToken);

            session.Schedule(command.StartTime, _dateTimeProvider);
            await _sessionRepository.UpdateAsync(session, cancellationToken);

            var result = new LiveSessionResult
            {
                Id = session.Id,
                UserId = session.HostUserId,
                StationId = session.AzuraCastStationId!.Value,
                StationName = session.AzuraCastStation?.StationName,
                SessionName = session.SessionName,
                Description = session.Description,
                Status = session.Status.ToString(),
                ScheduledStartAt = schedule.StartTime,
                StartedAt = null,
                EndedAt = null,
                CreatedAt = session.CreatedAt
            };

            return Result<LiveSessionResult>.Success(result);
        }
        catch (DomainException ex)
        {
            return Result<LiveSessionResult>.Failure(ex.Message, (ErrorCode)ex.StatusCode);
        }
    }
}
