using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead
{
    public class MarkAllNotificationsAsReadCommand : IRequest<bool>
    {
        public Guid UserId { get; set; }

        public MarkAllNotificationsAsReadCommand(Guid userId)
        {
            UserId = userId;
        }
    }
}
