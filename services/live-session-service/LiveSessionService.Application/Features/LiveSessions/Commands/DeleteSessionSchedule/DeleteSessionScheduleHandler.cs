using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.LiveSessions.Scheduling;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.DeleteSessionSchedule;

public sealed class DeleteSessionScheduleHandler : ICommandHandler<DeleteSessionScheduleCommand>
{
    private readonly ISessionScheduleRepository _scheduleRepository;
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteSessionScheduleHandler(
        ISessionScheduleRepository scheduleRepository,
        ILiveSessionRepository sessionRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _scheduleRepository = scheduleRepository;
        _sessionRepository = sessionRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteSessionScheduleCommand command, CancellationToken cancellationToken)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(command.ScheduleId, cancellationToken);
        if (schedule == null)
        {
            return Result.Failure("Schedule not found", ErrorCode.NotFound);
        }

        var session = await _sessionRepository.GetByIdAsync(schedule.LiveSessionId, cancellationToken);
        if (session == null)
        {
            return Result.Failure("Session not found", ErrorCode.NotFound);
        }

        await _scheduleRepository.DeleteAsync(schedule, cancellationToken);

        var remainingSchedules = await _scheduleRepository.GetByLiveSessionIdAsync(schedule.LiveSessionId, cancellationToken);
        var nextOccurrence = ScheduleOccurrenceCalculator.GetNextOccurrenceUtc(remainingSchedules, _dateTimeProvider.UtcNow);

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
            return Result.Failure(ex.Message, (ErrorCode)ex.StatusCode);
        }

        return Result.Success();
    }
}
