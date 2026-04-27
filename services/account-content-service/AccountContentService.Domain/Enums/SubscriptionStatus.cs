using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Domain.Enums
{
    public enum SubscriptionStatus
    {
        Pending = 0,
        Active = 1,
        Cancelled = 2,
        Expired = 3,
        Suspended = 4
    }
}
