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

        public string Method { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// Dynamic return URL passed from frontend for payment provider redirects.
        /// Falls back to config-based URL if not provided.
        /// </summary>
        public string? ReturnUrl { get; set; }

    }
}
