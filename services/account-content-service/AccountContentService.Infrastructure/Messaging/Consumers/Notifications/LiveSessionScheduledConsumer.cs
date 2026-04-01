using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
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
        public LiveSessionScheduledConsumer(INotificationRepository repo)
        {
            _repo = repo;
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

        }
    }
}
