using System;
using System.Collections.Generic;
using System.Text;

namespace shared.Contracts.Events.Notifications
{
    public class NotificationEvent
    {
        public string Title { get; set; }
        public Guid? SendUserId { get; set; }
        /// <summary>
        /// The user who receives this notification. Ignored when IsBroadcast = true.
        /// </summary>
        public Guid ReceiveUserId { get; set; }
        public Guid ReferenceId { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        /// <summary>
        /// When true, the notification is pushed to ALL connected users (e.g. new broadcast schedule).
        /// </summary>
        public bool IsBroadcast { get; set; } = false;
    }
}
