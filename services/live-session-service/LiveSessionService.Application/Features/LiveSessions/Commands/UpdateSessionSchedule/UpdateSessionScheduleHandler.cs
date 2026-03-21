using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.UpdateSessionSchedule;

public sealed class UpdateSessionScheduleHandler : ICommandHandler<UpdateSessionScheduleCommand, SessionScheduleResult>
{
    private readonly ISessionScheduleRepository _scheduleRepository;
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateSessionScheduleHandler(
        ISessionScheduleRepository scheduleRepository,
        ILiveSessionRepository sessionRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _scheduleRepository = scheduleRepository;
        _sessionRepository = sessionRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<SessionScheduleResult>> Handle(
        UpdateSessionScheduleCommand command,
        CancellationToken cancellationToken)
    {
        if (command.EndTime <= command.StartTime)
        {
            return Result<SessionScheduleResult>.Failure("End time must be greater than start time", ErrorCode.BadRequest);
        }

        var schedule = await _scheduleRepository.GetByIdAsync(command.ScheduleId, cancellationToken);
        if (schedule == null)
        {
            return Result<SessionScheduleResult>.Failure("Schedule not found", ErrorCode.NotFound);
        }

        var session = await _sessionRepository.GetByIdAsync(schedule.LiveSessionId, cancellationToken);
        if (session == null)
        {
            return Result<SessionScheduleResult>.Failure("Session not found", ErrorCode.NotFound);
        }

        try
        {
            session.Schedule(command.StartTime, _dateTimeProvider);
            await _sessionRepository.UpdateAsync(session, cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result<SessionScheduleResult>.Failure(ex.Message, (ErrorCode)ex.StatusCode);
        }

        schedule.StartTime = command.StartTime;
        schedule.EndTime = command.EndTime;
        if (!string.IsNullOrWhiteSpace(command.Title))
        {
            schedule.Title = command.Title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(command.Status))
        {
            schedule.Status = command.Status.Trim();
        }

        await _scheduleRepository.UpdateAsync(schedule, cancellationToken);

        return Result<SessionScheduleResult>.Success(new SessionScheduleResult
        {
            Id = schedule.Id,
            LiveSessionId = schedule.LiveSessionId,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            Title = schedule.Title,
            Status = schedule.Status,
            CreatedByUserId = null,
            UpdatedByUserId = command.ActorUserId
        });
    }
}
