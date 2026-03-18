using AccountContentService.Application.Features.Payments.Commands.CreatePayment;
using AccountContentService.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Infrastructure.Persistence
{
    public class PaymentGatewayFactory : IPaymentGateway
    {
        private readonly IEnumerable<IPaymentProvider> _providers;

        public PaymentGatewayFactory(IEnumerable<IPaymentProvider> providers)
        {
            _providers = providers;
        }

        public async Task<string> CreatePaymentUrlAsync(Guid orderId, CreatePaymentCommand request)
        {
            var provider = _providers.FirstOrDefault(p => p.Name.ToLower() == request.Method.ToLower());

            if (provider == null)
                throw new Exception("Payment method not supported");

            return await provider.CreatePaymentUrlAsync(orderId, request);
        }
    }
}
