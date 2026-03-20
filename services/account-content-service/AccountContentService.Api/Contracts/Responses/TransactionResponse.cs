namespace AccountContentService.Api.Contracts.Responses
{
    public class TransactionResponse
    {
        public Guid Id { get; set; }

        public Guid PaymentId { get; set; }

        public string PaymentProvider { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public DateTime PaymentAt { get; set; }

        public string TransactionStatus { get; set; }

        public DateTime CreatedAt { get; set; }

    }
}
