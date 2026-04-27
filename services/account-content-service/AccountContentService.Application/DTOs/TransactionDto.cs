using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.DTOs
{
    public class TransactionDto
    {
        public Guid Id { get; set; }

        public Guid PaymentId { get; set; }

        public string PaymentProvider { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public DateTime PaymentAt { get; set; }

        public string TransactionStatus { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // What was purchased
        public string TargetType { get; set; } = string.Empty;
        public Guid? TargetId { get; set; }

        public UserProfileDto userProfile { get; set; } = null!;
    }
}
