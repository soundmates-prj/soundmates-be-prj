using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.LiveSessions.Scheduling;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Enums;
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

        if (command.EndDate.HasValue && command.EndDate.Value < command.StartDate)
        {
            return Result<SessionScheduleResult>.Failure("End date cannot be earlier than start date", ErrorCode.BadRequest);
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        var todayUtc = DateOnly.FromDateTime(nowUtc);
        var nowTimeUtc = TimeOnly.FromDateTime(nowUtc);

        if (command.StartDate == todayUtc &&
            (command.StartTime <= nowTimeUtc || command.EndTime <= nowTimeUtc))
        {
            return Result<SessionScheduleResult>.Failure(
                "For today schedule, start time and end time must be greater than current time",
                ErrorCode.BadRequest);
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

        var isRecurring = command.IsRecurring ?? schedule.IsRecurring;
        var daysOfWeek = command.DaysOfWeek ?? schedule.DaysOfWeek;

        if (isRecurring && daysOfWeek == DaysOfWeek.None)
        {
            return Result<SessionScheduleResult>.Failure("At least one day of week is required for recurring schedules", ErrorCode.BadRequest);
        }

        schedule.StartTime = command.StartTime;
        schedule.EndTime = command.EndTime;
        schedule.StartDate = command.StartDate;
        schedule.EndDate = command.EndDate;

        if (!string.IsNullOrWhiteSpace(command.Title))
        {
            schedule.Title = command.Title.Trim();
        }

        schedule.IsRecurring = isRecurring;
        schedule.DaysOfWeek = isRecurring ? daysOfWeek : DaysOfWeek.None;
        schedule.UpdatedBy = command.ActorUserId;

        var allSchedules = await _scheduleRepository.GetByLiveSessionIdAsync(schedule.LiveSessionId, cancellationToken);
        var nextOccurrence = ScheduleOccurrenceCalculator.GetNextOccurrenceUtc(allSchedules, _dateTimeProvider.UtcNow);

        await _scheduleRepository.UpdateAsync(schedule, cancellationToken);

        try
        {
            if (nextOccurrence.HasValue)
            {
                session.Schedule(nextOccurrence.Value, _dateTimeProvider);
            }
            else if (session.Status == SessionStatus.Scheduled)
            {
                session.RevertToCreated(_dateTimeProvider);
            }

            await _sessionRepository.UpdateAsync(session, cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result<SessionScheduleResult>.Failure(ex.Message, (ErrorCode)ex.StatusCode);
        }

        return Result<SessionScheduleResult>.Success(new SessionScheduleResult
        {
            Id = schedule.Id,
            LiveSessionId = schedule.LiveSessionId,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            Title = schedule.Title,
            Status = schedule.Status.ToString(),
            IsRecurring = schedule.IsRecurring,
            DaysOfWeek = schedule.DaysOfWeek,
            StartDate = schedule.StartDate,
            EndDate = schedule.EndDate,
            CreatedBy = schedule.CreatedBy,
            UpdatedBy = schedule.UpdatedBy
        });
    }
}
