using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Contracts.Events.Notifications
{
    public class LiveSessionScheduledEvent
    {
        public Guid SessionId { get; set; }
        public Guid HostId { get; set; }
        public DateTime StartTime { get; set; }
    }
}
