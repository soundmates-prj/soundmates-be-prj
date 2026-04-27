using AccountContentService.Domain.Enums;

namespace AccountContentService.Api.Contracts.Requests
{
    public class PaymentRequest
    {
        public string TargetType { get; set; } = string.Empty;

        public Guid TargetId { get; set; }

        public string Method { get; set; }

        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Frontend return URL for payment provider redirects after completion.
        /// Optional - falls back to server-side config if not provided.
        /// </summary>
        public string? ReturnUrl { get; set; }
    }
}
