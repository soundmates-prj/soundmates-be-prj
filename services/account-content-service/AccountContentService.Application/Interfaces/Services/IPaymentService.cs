using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Interfaces.Services
{
    public interface IPaymentService
    {
        bool VNPayVerifySignature(Dictionary<string, string> data);
    }
}
