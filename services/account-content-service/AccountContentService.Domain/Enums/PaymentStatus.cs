using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Domain.Enums
{
    public enum PaymentStatus
    {
        Pending,
        Success,
        Failed,
        Cancelled
    }
    public enum PaymentTransactionStatus
    {
        Pending,
        Success,
        Failed,
        Expired
    }
}
