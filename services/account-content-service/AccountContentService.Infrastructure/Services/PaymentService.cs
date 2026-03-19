using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Infrastructure.Configurations;
using AccountContentService.Infrastructure.Integrations.PaymentGateway;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly VNPayConfig _vnPayconfig;

        public PaymentService(IOptions<VNPayConfig> vnPayconfig)
        {
            _vnPayconfig = vnPayconfig.Value;
        }
        public bool VNPayVerifySignature(Dictionary<string, string> data)
        {
            return VNPayHelper.VerifySignature(data, _vnPayconfig.HashSecret);
        }
    }
}
