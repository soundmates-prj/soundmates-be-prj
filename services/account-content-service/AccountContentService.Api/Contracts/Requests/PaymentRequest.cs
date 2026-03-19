using AccountContentService.Domain.Enums;

namespace AccountContentService.Api.Contracts.Requests
{
    public class PaymentRequest
    {
        public string TargetType { get; set; } = string.Empty;

        public Guid TargetId { get; set; }

        public string Method { get; set; }

        public decimal TotalAmount { get; set; }

    }
}
