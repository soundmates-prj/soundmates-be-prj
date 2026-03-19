using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.Payments.Commands.CreatePayment
{
    public class CreatePaymentCommand : IRequest<string>
    {

        public Guid UserId { get; set; }

        public string TargetType { get; set; } = string.Empty;

        public Guid TargetId { get; set; }

        public string Method { get; set; }

        public decimal TotalAmount { get; set; }

        public string IpAddress { get; set; } = string.Empty;

    }
}
