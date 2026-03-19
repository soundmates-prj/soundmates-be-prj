using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Domain.Enums
{
    public enum PaymentProvider
    {
        VNPay,
        PayOS
    }

    public enum PaymentMethod
    {
        BankTransfer,
        QR,
        Wallet
    }

    public enum PaymentTargetType
    {
        Subscription,
        LiveSession,
        PodcastLetter
    }
}
