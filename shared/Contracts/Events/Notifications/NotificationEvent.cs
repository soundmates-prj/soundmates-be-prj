using System;
using System.Collections.Generic;
using System.Text;

namespace shared.Contracts.Events.Notifications
{
    public class NotificationEvent
    {
        public string Title { get; set; }
        public Guid? SendUserId { get; set; }
        public Guid ReceiveUserId { get; set; }
        public Guid ReferenceId { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
    }
}
