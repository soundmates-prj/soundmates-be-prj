using AccountContentService.Application.Features.Payments.Commands.CreatePayment;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Interfaces.Services
{
    public interface IPaymentGateway
    {
        Task<string> CreatePaymentUrlAsync(Guid orderId, CreatePaymentCommand command);
    }
}
