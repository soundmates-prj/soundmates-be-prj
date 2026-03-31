using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.LiveSessions.Scheduling;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.CreateSessionSchedule;

public sealed class CreateSessionScheduleHandler : ICommandHandler<CreateSessionScheduleCommand, SessionScheduleResult>
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

    public async Task<Result<SessionScheduleResult>> Handle(
        CreateSessionScheduleCommand command,
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
            var scheduleNow = ConvertUtcToScheduleLocal(nowUtc);
            var today = DateOnly.FromDateTime(scheduleNow);
            var nowTime = TimeOnly.FromDateTime(scheduleNow);

            if (command.StartDate == today &&
                (command.StartTime <= nowTime || command.EndTime <= nowTime))
            {
                return Result<SessionScheduleResult>.Failure(
                    "For today schedule, start time and end time must be greater than current time",
                    ErrorCode.BadRequest);
            }

        if (command.IsRecurring && command.DaysOfWeek == DaysOfWeek.None)
        {
            return Result<SessionScheduleResult>.Failure("At least one day of week is required for recurring schedules", ErrorCode.BadRequest);
        }

        var session = await _sessionRepository.GetByIdWithStationAsync(command.LiveSessionId, cancellationToken);
        if (session == null)
        {
            return Result<SessionScheduleResult>.Failure("Session not found", ErrorCode.NotFound);
        }

        var normalizedDaysOfWeek = command.IsRecurring ? command.DaysOfWeek : DaysOfWeek.None;
        var existingSchedules = await _scheduleRepository.GetByLiveSessionIdAsync(command.LiveSessionId, cancellationToken);

        var isDuplicate = existingSchedules.Any(x =>
            x.StartTime == command.StartTime &&
            x.EndTime == command.EndTime &&
            x.StartDate == command.StartDate &&
            x.EndDate == command.EndDate &&
            x.IsRecurring == command.IsRecurring &&
            x.DaysOfWeek == normalizedDaysOfWeek);

        if (isDuplicate)
        {
            return Result<SessionScheduleResult>.Failure(
                "A schedule with the same time and recurrence already exists",
                ErrorCode.Conflict);
        }

        var schedule = new SessionSchedule
        {
            Id = Guid.NewGuid(),
            LiveSessionId = session.Id,
            StartTime = command.StartTime,
            EndTime = command.EndTime,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Title = string.IsNullOrWhiteSpace(command.Title) ? session.SessionName : command.Title.Trim(),
            Status = ScheduleStatus.Scheduled,
            IsRecurring = command.IsRecurring,
            DaysOfWeek = normalizedDaysOfWeek,
            CreatedBy = command.ActorUserId,
            UpdatedBy = null,
            CreatedAt = _dateTimeProvider.UtcNow
        };

        var allSchedules = existingSchedules.Append(schedule);
        var nextOccurrence = ScheduleOccurrenceCalculator.GetNextOccurrenceUtc(allSchedules, _dateTimeProvider.UtcNow);
        if (!nextOccurrence.HasValue)
        {
            return Result<SessionScheduleResult>.Failure(
                "Schedule does not have any upcoming occurrence",
                ErrorCode.BadRequest);
        }

        await _scheduleRepository.AddAsync(schedule, cancellationToken);

        try
        {
            session.Schedule(nextOccurrence.Value, _dateTimeProvider);
            await _sessionRepository.UpdateAsync(session, cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result<SessionScheduleResult>.Failure(ex.Message, (ErrorCode)ex.StatusCode);
        }

        return Result<SessionScheduleResult>.Success(MapToResult(schedule, session));
    }

    private static SessionScheduleResult MapToResult(SessionSchedule schedule, LiveSession session) => new()
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
        UpdatedBy = schedule.UpdatedBy,
        CreatedAt = schedule.CreatedAt,
        LiveSession = new LiveSessionScheduleData
        {
            Id = session.Id,
            SessionName = session.SessionName,
            Description = session.Description,
            Status = session.Status.ToString(),
            HostUserId = session.HostUserId,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            Genre = session.Genre,
            ThumbnailUrl = session.ThumbnailUrl,
            Station = session.AzuraCastStation != null ? new StationScheduleData
            {
                Id = session.AzuraCastStation.Id,
                ExternalStationId = session.AzuraCastStation.ExternalStationId,
                StationName = session.AzuraCastStation.StationName,
                StationShortcode = session.AzuraCastStation.StationShortcode,
                Description = session.AzuraCastStation.Description,
                StreamUrl = session.AzuraCastStation.StreamUrl,
                PublicPlayerUrl = session.AzuraCastStation.PublicPlayerUrl
            } : null
        }
    };
}
