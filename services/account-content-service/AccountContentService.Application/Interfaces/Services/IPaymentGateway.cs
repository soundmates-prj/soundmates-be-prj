using AccountContentService.Application.Features.Payments.Commands.CreatePayment;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AccountContentService.Application.Interfaces.Services
{
    public interface IPaymentGateway
    {
        Task<string> CreatePaymentUrlAsync(Guid orderId, CreatePaymentCommand command);
        Task<bool> ExecutePayoutAsync(Guid payoutId, decimal amount, string bankId, string accountNumber, string accountName, string description, string provider = "sepay");
    }
}
