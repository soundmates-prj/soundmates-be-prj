using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Notifications.Commands.MarkNotificationAsRead
{
    public class MarkNotificationAsReadCommand : IRequest<bool>
    {
        public Guid NotificationId { get; set; }

        public MarkNotificationAsReadCommand(Guid notificationId)
        {
            NotificationId = notificationId;
        }
    }
}
