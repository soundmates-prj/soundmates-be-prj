using AccountContentService.Application.Interfaces;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Shared.Contracts.Events.Notifications;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Infrastructure.Messaging.Consumers.Notifications
{
    public class LiveSessionScheduledConsumer
    {
        private readonly INotificationRepository _repo;
        private readonly INotificationPusher _notificationPusher;
        private readonly ILogger<LiveSessionScheduledConsumer> _logger;

        public LiveSessionScheduledConsumer(
            INotificationRepository repo,
            INotificationPusher notificationPusher,
            ILogger<LiveSessionScheduledConsumer> logger)
        {
            _repo = repo;
            _notificationPusher = notificationPusher;
            _logger = logger;
        }

        public async Task Handle(
            LiveSessionScheduledEvent evt,
            CancellationToken cancellationToken)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = evt.HostId,
                Title = "Live Session Scheduled",
                ReferenceId = evt.SessionId,
                Type = "live_session",
                Message = $"Your session is scheduled at {evt.StartTime:HH:mm dd/MM/yyyy}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(notification, cancellationToken);

            // Push notification to user in real-time via SignalR
            try
            {
                await _notificationPusher.PushToUserAsync(evt.HostId, notification, cancellationToken);
                _logger.LogInformation("Pushed live session notification {NotificationId} to user {UserId}", notification.Id, evt.HostId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to push live session notification {NotificationId} to user {UserId}", notification.Id, evt.HostId);
            }
        }
    }
}
