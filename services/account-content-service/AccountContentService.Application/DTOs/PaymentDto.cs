using AccountContentService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.DTOs
{
    public class PaymentRequestDto
    {
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }

        public PaymentMethod Method { get; set; }
    }

    public class PaymentResponse
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid PlanId { get; set; }

        public string TargetType { get; set; } = string.Empty;

        public Guid TargetId { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "pending";

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string? ExternalReference { get; set; }
    }
}
