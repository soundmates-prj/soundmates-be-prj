using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.LiveSessions.Scheduling;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.StopSession;

public sealed class StopSessionHandler : ICommandHandler<StopSessionCommand, LiveSessionResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly ISessionScheduleRepository _scheduleRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<StopSessionHandler> _logger;
    private readonly ILiveSessionNotifier _notifier;

    public StopSessionHandler(
        ILiveSessionRepository sessionRepository,
        ISessionScheduleRepository scheduleRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<StopSessionHandler> logger,
        ILiveSessionNotifier notifier)
    {
        _sessionRepository = sessionRepository;
        _scheduleRepository = scheduleRepository;
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

            var endedRunStartedAt = session.StartedAt;
            var endedRunEndedAt = session.EndedAt;

            var schedules = await _scheduleRepository.GetByLiveSessionIdAsync(session.Id, cancellationToken);
            var nowUtc = _dateTimeProvider.UtcNow;

            var oneTimeToComplete = schedules
                .Where(x => !x.IsRecurring && x.Status == ScheduleStatus.Scheduled)
                .Where(x => DateTime.SpecifyKind(x.StartDate.ToDateTime(x.StartTime), DateTimeKind.Utc) <= nowUtc)
                .ToList();

            // If session was manually started before scheduled time, still consume one-time schedule for that run date
            if (oneTimeToComplete.Count == 0 && endedRunStartedAt.HasValue)
            {
                var runDate = DateOnly.FromDateTime(endedRunStartedAt.Value);
                var sameDayOneTime = schedules
                    .Where(x => !x.IsRecurring && x.Status == ScheduleStatus.Scheduled && x.StartDate == runDate)
                    .OrderBy(x => x.StartTime)
                    .FirstOrDefault();

                if (sameDayOneTime != null)
                {
                    oneTimeToComplete.Add(sameDayOneTime);
                }
            }

            foreach (var item in oneTimeToComplete)
            {
                item.Status = ScheduleStatus.Completed;
                item.UpdatedBy ??= item.CreatedBy;
                await _scheduleRepository.UpdateAsync(item, cancellationToken);
            }

            var nextOccurrence = ScheduleOccurrenceCalculator.GetNextOccurrenceUtc(schedules, nowUtc);
            if (nextOccurrence.HasValue)
            {
                try
                {
                    var safeNextOccurrence = ScheduleOccurrenceCalculator.GetNextOccurrenceUtc(
                        schedules,
                        _dateTimeProvider.UtcNow);

                    if (safeNextOccurrence.HasValue)
                    {
                        session.Schedule(safeNextOccurrence.Value, _dateTimeProvider);
                        await _sessionRepository.UpdateAsync(session, cancellationToken);
                    }
                }
                catch (DomainException ex) when (ex.Message.Contains("Scheduled start time must be in the future", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "Skipping auto-reschedule for session {SessionId} because next occurrence is no longer in the future",
                        session.Id);
                }
            }

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
                StartedAt = endedRunStartedAt,
                EndedAt = endedRunEndedAt,
                TotalDuration = endedRunEndedAt.HasValue && endedRunStartedAt.HasValue
                    ? (int)(endedRunEndedAt.Value - endedRunStartedAt.Value).TotalMinutes
                    : 0,
                CreatedAt = session.CreatedAt,
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
