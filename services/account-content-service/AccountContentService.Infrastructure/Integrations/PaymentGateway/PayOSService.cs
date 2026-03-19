using AccountContentService.Application.Features.Payments.Commands.CreatePayment;
using AccountContentService.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Infrastructure.NotificationService.PaymentGateway
{
    public class PayOSService : IPaymentProvider
    {
        public string Name => "payos";

        public async Task<string> CreatePaymentUrlAsync(Guid orderId, CreatePaymentCommand request)
        {
            // call PayOS API
            return "https://payos.vn/payment-url";
        }
    }
}
