using System.Linq;
using System.Text.Json;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Interfaces;
using shared.Contracts.Events.Notifications;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.CancelSession;

public sealed class CancelSessionHandler : ICommandHandler<CancelSessionCommand, LiveSessionResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly ISessionScheduleRepository _scheduleRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMessageBusPublisher _eventBus;

    public CancelSessionHandler(
        ILiveSessionRepository sessionRepository,
        ISessionScheduleRepository scheduleRepository,
        IDateTimeProvider dateTimeProvider,
        IMessageBusPublisher eventBus)
    {
        _sessionRepository = sessionRepository;
        _scheduleRepository = scheduleRepository;
        _dateTimeProvider = dateTimeProvider;
        _eventBus = eventBus;
    }

    public async Task<Result<LiveSessionResult>> Handle(
        CancelSessionCommand command,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdWithStationAsync(command.SessionId, cancellationToken);
        if (session == null)
        {
            return Result<LiveSessionResult>.Failure("Session not found", ErrorCode.NotFound);
        }

        try
        {
            session.Cancel(_dateTimeProvider);
            await _sessionRepository.UpdateAsync(session, cancellationToken);

            var schedules = await _scheduleRepository.GetByLiveSessionIdAsync(session.Id, cancellationToken);
            var scheduledItems = schedules
                .Where(x => x.Status == ScheduleStatus.Scheduled)
                .ToList();

            foreach (var schedule in scheduledItems)
            {
                schedule.Status = ScheduleStatus.Cancelled;
                schedule.UpdatedBy = command.ActorUserId;
                await _scheduleRepository.UpdateAsync(schedule, cancellationToken);
            }

            var @event = new NotificationEvent
            {
                Title = "Thông báo phiên live",
                SendUserId = command.ActorUserId,
                ReceiveUserId = Guid.Empty,
                ReferenceId = session.Id,
                Type = "live_session",
                Message = $"{session.SessionName} dự kiến diễn ra đã bị hủy bỏ. Rất tiếc vì sự bất tiện này.",
                IsBroadcast = true
            };

            var payload = JsonSerializer.Serialize(@event);
            await _eventBus.PublishAsync("notification.created", payload);

            return Result<LiveSessionResult>.Success(new LiveSessionResult
            {
                Id = session.Id,
                UserId = session.HostUserId,
                StationId = session.AzuraCastStationId!.Value,
                StationName = session.AzuraCastStation?.StationName,
                SessionName = session.SessionName,
                Description = session.Description,
                Status = session.Status.ToString(),
                ScheduledStartAt = null,
                StartedAt = null,
                EndedAt = null,
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
