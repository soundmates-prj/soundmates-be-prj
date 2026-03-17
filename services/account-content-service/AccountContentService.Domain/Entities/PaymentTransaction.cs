namespace AccountContentService.Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public string PaymentProvider { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime PaymentAt { get; set; }

    public string? ResponsePayload { get; set; }

    public string TransactionStatus { get; set; } = "pending";

    public DateTime? ExpiredAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Payment Payment { get; set; } = null!;
}