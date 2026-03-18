using AccountContentService.Application.Features.Payments.Commands.CreatePayment;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Interfaces.Services
{
    public interface IPaymentProvider
    {
        string Name { get; }
        Task<string> CreatePaymentUrlAsync(Guid orderId, CreatePaymentCommand request);
    }
}
